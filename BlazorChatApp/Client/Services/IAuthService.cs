﻿using BlazorChatApp.Shared.Models;
using System.Threading.Tasks;

namespace BlazorChatApp.Client.Services
{
    public interface IAuthService
    {
        Task<LoginResult> Login(LoginModel loginModel);
        Task Logout();
        Task<RegisterResult> Register(RegisterModel registerModel);
    }
}
