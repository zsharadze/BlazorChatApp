using BlazorChatApp.Client.Services;
using BlazorChatApp.Client.Shared;
using BlazorChatApp.Shared.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using static System.Net.WebRequestMethods;
using System.Net.Http.Json;
using System.Security.Principal;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using System.Data.Common;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorChatApp.Client.Pages
{
    public class IndexModalBase : ComponentBase
    {
        public List<ChatUser> ConnectedUsers = new List<ChatUser>();
        public List<ChatUser> FilteredUsers = new List<ChatUser>();
        public List<ChatMessage> SelectedUserChatHistory = new List<ChatMessage>();
        [Inject]
        private AuthenticationStateProvider GetAuthenticationStateAsync { get; set; }
        [Inject]
        private IAuthService AuthService { get; set; }
        public string CurrentUsername { get; set; }
        public string CurrentUserId { get; set; }
        [Inject]
        private HubConnection HubConnection { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; }
        [Inject]
        public ILocalStorageService LocalStorage { get; set; }

        [Inject]
        public HttpClient HttpClient { get; set; }
        public ChatUser SelectedUser { get; set; }
        public string InputMessage { get; set; }
        public string InputSearch { get; set; }
        [Inject]
        IJSRuntime JS { get; set; }
        public ElementReference ChatHistoryDiv;

        protected override async Task OnInitializedAsync()
        {
            var authstate = await GetAuthenticationStateAsync.GetAuthenticationStateAsync();
            CurrentUsername = authstate.User.Identity.Name;
            CurrentUserId = await LocalStorage.GetItemAsync<string>("userId");
            //var savedToken = await LocalStorage.GetItemAsync<string>("authToken");
            //this.HubConnection = new HubConnectionBuilder()
            //     .WithUrl(NavigationManager.ToAbsoluteUri("/chat"))
            ////.WithUrl(NavigationManager.ToAbsoluteUri("/chat"), options =>
            ////{
            ////    options.AccessTokenProvider = () => Task.FromResult(savedToken);
            ////})
            //.Build();
            this.HubConnection.On<List<ChatUser>>("ReceiveConnectedUsers",
            async cus =>
            {
                cus.Remove(cus.SingleOrDefault(x => x.Username == CurrentUsername));

                var unreadMessages = await HttpClient.GetFromJsonAsync<Dictionary<string, int>>("Chat/GetUnreadMessages");

                foreach (var item in unreadMessages)
                {
                    var cu = cus.SingleOrDefault(x => x.UserId == item.Key);

                    if (cu != null)
                    {
                        cu.UnreadMessageCount = item.Value;
                    }
                }

                ConnectedUsers = cus;
                FilteredUsers = cus;


                StateHasChanged();
            });
            this.HubConnection.On<ChatUser>("UserConnected",
            u =>
            {
                if (u.Username != CurrentUsername)
                {
                    var user = ConnectedUsers.SingleOrDefault(x => x.Username == u.Username);
                    if (user != null)
                    {
                        user.IsOnline = true;
                    }
                    else
                    {
                        ConnectedUsers.Add(u);
                    }
                    ConnectedUsers = ConnectedUsers.OrderByDescending(x => x.IsOnline).ThenByDescending(x => x.UnreadMessageCount).ToList();
                }
                StateHasChanged();
            });
            this.HubConnection.On<string>("UserDisconnected",
            m =>
            {
                if (ConnectedUsers.Any(x => x.Username == m))
                {
                    var user = ConnectedUsers.SingleOrDefault(x => x.Username == m);
                    user.IsOnline = false;
                }
                StateHasChanged();
            });
            this.HubConnection.On<ChatMessage>("ReceiveMessage",
            m =>
            {
                if (m.SenderUserId == SelectedUser?.UserId)
                {
                    foreach (var item in ConnectedUsers)
                    {
                        if (item.UserId == m.SenderUserId)
                        {
                            item.UnreadMessageCount++;
                        }
                    }
                    ConnectedUsers = ConnectedUsers.OrderByDescending(x => x.IsOnline).ThenByDescending(x => x.UnreadMessageCount).ToList();
                    SelectedUserChatHistory.Add(m);
                    StateHasChanged();
                }
            });

            this.HubConnection.On<ChatMessage>("ReceiveImgMessage",
                       m =>
                       {
                           foreach (var item in ConnectedUsers)
                           {
                               if (item.UserId == m.SenderUserId)
                               {
                                   item.UnreadMessageCount++;
                               }
                           }
                           ConnectedUsers = ConnectedUsers.OrderByDescending(x => x.IsOnline).ThenByDescending(x => x.UnreadMessageCount).ToList();
                           SelectedUserChatHistory.Add(m);
                           StateHasChanged();
                       });

            this.HubConnection.On<ChatMessage>("ReceiveSendedImgMessageToSender",
                      m =>
                      {
                          SelectedUserChatHistory.Add(m);
                          StateHasChanged();
                      });

            if (this.HubConnection.State == HubConnectionState.Disconnected)
                await this.HubConnection.StartAsync();
            await this.HubConnection.SendAsync("GetUsers");
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (SelectedUserChatHistory.Any())
                ScrollToEnd();
            return base.OnAfterRenderAsync(firstRender);
        }

        protected async void Logout()
        {
            await AuthService.Logout();
        }

        public async Task SelectUser(ChatUser chatUser)
        {
            SelectedUser = chatUser;
            SelectedUserChatHistory = new List<ChatMessage>();
            SelectedUserChatHistory = await HttpClient.GetFromJsonAsync<List<ChatMessage>>("Chat/GetChatHistory/?currentUserId=" + CurrentUserId + "&selectedUserId=" + SelectedUser.UserId);
            ScrollToEnd();
        }

        public async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputMessage) || SelectedUser == null)
            {
                return;
            }

            ChatMessage messageToAdd = new ChatMessage();
            messageToAdd.Message = InputMessage;
            messageToAdd.ReceiverUserId = SelectedUser.UserId;
            messageToAdd.SenderUserId = CurrentUserId;
            messageToAdd.SendDate = DateTime.Now;

            SelectedUserChatHistory.Add(messageToAdd);
            InputMessage = "";
            await this.HubConnection.SendAsync("SendMessage", messageToAdd);
            StateHasChanged();
            ScrollToEnd();
        }

        public async void Send(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" || e.Code == "NumpadEnter")
            {
                await SendMessage();
            }
        }

        async void ScrollToEnd()
        {
            await JS.InvokeVoidAsync("scrollChatToEnd", new object[] { ChatHistoryDiv });
        }

        public async void OpenFileUpload()
        {
            await JS.InvokeVoidAsync("triggerImgUpload", new object[] { });
        }

        public async Task InputMessageClicked()
        {
            if (SelectedUser != null)
            {
                var readFromUser = ConnectedUsers.SingleOrDefault(x => x.UserId == SelectedUser.UserId);

                readFromUser.UnreadMessageCount = 0;
                CurrentUserAndSenderDTO currentUserAndSenderDTO = new CurrentUserAndSenderDTO();
                currentUserAndSenderDTO.CurrentUserId = CurrentUserId;
                currentUserAndSenderDTO.SelectedUserId = SelectedUser.UserId;
                await HubConnection.SendAsync("ReadMessage", currentUserAndSenderDTO);
            }
        }

        public async Task ImgUpload(InputFileChangeEventArgs e)
        {
            int maxSizeOfFile = 1024 * 10240;//max file size 10mb
            MemoryStream ms = new MemoryStream();
            await e.File.OpenReadStream(maxSizeOfFile).CopyToAsync(ms);
            var bytes = ms.ToArray();
            var fileExtension = Path.GetExtension(e.File.Name);

            var imgMessage = new ChatMessage();
            imgMessage.ImgBytes = bytes;
            imgMessage.ImgFileExtension = fileExtension;
            imgMessage.IsImgMessage = true;
            imgMessage.ReceiverUserId = SelectedUser.UserId;
            imgMessage.SenderUserId = CurrentUserId;
            imgMessage.Message = "-";

            await HubConnection.SendAsync("SendImgMessage", imgMessage);
        }

        public void SearchUser()
        {
            FilteredUsers = ConnectedUsers.Where(x => x.Username.Contains(InputSearch)).ToList();
        }
    }
}
