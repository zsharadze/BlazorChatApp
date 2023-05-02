using BlazorChatApp.Server.Data;
using BlazorChatApp.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Net;
using System.Security.Claims;

namespace BlazorChatApp.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatHubClient>
    {
        private static Dictionary<string, ChatUser> _connectedUsers;
        private static List<ChatUser> _usersList = new List<ChatUser>();
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ChatHub(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;

            if (_usersList != null && !_usersList.Any())
                _usersList = _dbContext.Users.AsNoTracking().Select(x => new ChatUser()
                {
                    Username = x.UserName,
                    Avatar = x.Avatar,
                    IsOnline = false,
                    UserId = x.Id,
                    UnreadMessageCount = 0
                }).ToList();
            if (_connectedUsers == null)
            {
                _connectedUsers = new Dictionary<string, ChatUser>();
            }
        }

        public string CurrentUser
        {
            get
            {
                return Context.User.Identity.Name;
            }
        }

        public string CurrentUserId
        {
            get
            {
                return Context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }

        public override Task OnConnectedAsync()
        {
            ChatUser curUser = _usersList.SingleOrDefault(x => x.Username == CurrentUser);

            if (curUser == null)
            {
                var newUser = _dbContext.Users.Single(x => x.UserName == CurrentUser);
                curUser = new ChatUser() { UserId = newUser.Id, Username = newUser.UserName, IsOnline = true, Avatar = newUser.Avatar, UnreadMessageCount = 0 };
                _usersList.Add(curUser);
            }
            var connectedUser = _connectedUsers.Values.SingleOrDefault(x => x.Username == CurrentUser);

            if (connectedUser == null)
            {
                curUser.IsOnline = true;
                _connectedUsers.Add(Context.ConnectionId, curUser);
            }

            Clients.All.UserConnected(curUser);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_connectedUsers.Any(x => x.Key == Context.ConnectionId))
                _connectedUsers.Remove(Context.ConnectionId);
            var user = _usersList.SingleOrDefault(x => x.Username == CurrentUser);
            user.IsOnline = false;
            Clients.All.UserDisconnected(CurrentUser);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetUsers()
        {
            var user = _usersList.SingleOrDefault(x => x.Username == CurrentUser);
            user.IsOnline = true;
            var uList = _usersList.Where(x => x.Username != CurrentUser).OrderByDescending(x => x.IsOnline).ToList();

            await Clients.Caller.ReceiveConnectedUsers(uList);
        }

        public async Task SendMessage(ChatMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.SenderUserId) || string.IsNullOrWhiteSpace(message.ReceiverUserId) || string.IsNullOrWhiteSpace(message.Message))
            {
                throw new ArgumentException("Invalid paramters");
            }

            message.SendDate = DateTime.Now;

            await _dbContext.ChatMessage.AddAsync(message);
            await _dbContext.SaveChangesAsync();

            var receiverUser = _connectedUsers.SingleOrDefault(x => x.Value.UserId == message.ReceiverUserId);

            await Clients.Client(receiverUser.Key).ReceiveMessage(message);
        }

        /// <summary>
        /// sends image to receiver
        /// </summary>
        /// <param name="imgMessage"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SendImgMessage(ChatMessage imgMessage)
        {
            if (imgMessage.ImgBytes == null || imgMessage.ImgBytes.Length == 0)
            {
                throw new Exception("Image is null");
            }
            else if (imgMessage.SenderUserId != CurrentUserId)
            {
                throw new Exception("Invalid SenderUserId");
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(imgMessage.ImgBytes))
                    Image.FromStream(ms);
            }
            catch (Exception ex)
            {
                throw new Exception("File is not an image. " + ex.ToString());
            }
            imgMessage.Message = "";

            string imgsMessageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "imgsMessage");
            var fileName = Guid.NewGuid().ToString() + imgMessage.ImgFileExtension;
            string filePath = Path.Combine(imgsMessageFolder, fileName);

            using var writer = new BinaryWriter(System.IO.File.OpenWrite(filePath));
            writer.Write(imgMessage.ImgBytes);

            imgMessage.IsImgMessage = true;
            imgMessage.ImgFileName = fileName;
            imgMessage.SendDate = DateTime.Now;

            await _dbContext.ChatMessage.AddAsync(imgMessage);
            await _dbContext.SaveChangesAsync();

            var receiverUser = _connectedUsers.SingleOrDefault(x => x.Value.UserId == imgMessage.ReceiverUserId);
            var senderUser = _connectedUsers.SingleOrDefault(x => x.Value.UserId == imgMessage.SenderUserId);
            imgMessage.ImgBytes = null;
            await Clients.Client(receiverUser.Key).ReceiveImgMessage(imgMessage);
            await Clients.Client(senderUser.Key).ReceiveSendedImgMessageToSender(imgMessage);//send response to sender to reflect img in its chat
        }

        /// <summary>
        /// when user clicks chat message input button this method sets to read all messages from selected user.
        /// </summary>
        /// <param name="currentUserAndSenderDTO"></param>
        /// <returns></returns>
        public async Task ReadMessage(CurrentUserAndSenderDTO currentUserAndSenderDTO)
        {
            var result = await _dbContext.ChatMessage
                .Where(x => (x.SenderUserId == currentUserAndSenderDTO.SelectedUserId && x.ReceiverUserId == currentUserAndSenderDTO.CurrentUserId))
                .ToListAsync();

            foreach (var item in result)
            {
                item.ReadByReceiver = true;
            }

            if (result.Any())
                await _dbContext.SaveChangesAsync();
        }
    }
}
