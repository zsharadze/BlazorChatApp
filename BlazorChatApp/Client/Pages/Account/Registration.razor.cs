using BlazorChatApp.Client.Services;
using BlazorChatApp.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Net;
using System.Net.Http.Json;

namespace BlazorChatApp.Client.Pages.Account
{
    public class RegistrationModalBase : ComponentBase
    {
        [Inject]
        NavigationManager NavigationManager { get; set; }
        [Inject]
        IAuthService AuthService { get; set; }

        protected BlazorChatApp.Shared.Models.RegisterModel registration = new();
        protected string Error { get; set; }

        protected override Task OnInitializedAsync()
        {
            registration.Avatar = ((Avatar[])Enum.GetValues(typeof(Avatar))).First();
            return base.OnInitializedAsync();
        }

        protected async Task RegisterUser()
        {
            try
            {
                var result = await AuthService.Register(registration);

                if (result.Success)
                {
                    NavigationManager.NavigateTo("/account/login/true");
                }
                else
                {
                    Error = result.Message;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected void RedirectToLogin()
        {
            NavigationManager.NavigateTo("/account/login");
        }
    }
}
