using BlazorChatApp.Client.Services;
using BlazorChatApp.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;

namespace BlazorChatApp.Client.Pages.Account
{
    public class LoginModalBase : ComponentBase
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IAuthService AuthService { get; set; }
        protected BlazorChatApp.Shared.Models.LoginModel login = new();
        protected string Error { get; set; }
        [Parameter]
        public bool RegSuccess { get; set; }

        protected async Task LoginUser()
        {
            try
            {
                var result = await AuthService.Login(login);
                if (result.Success)
                {
                    NavigationManager.NavigateTo("/chat");
                }
                else
                {
                    Error = result.Message;
                }

            }
            catch (Exception ex)
            {
                //Logger.LogError(ex.Message);
                throw ex;
            }
        }

        protected void RedirectToRegister()
        {
            NavigationManager.NavigateTo("/account/register");
        }
    }
}
