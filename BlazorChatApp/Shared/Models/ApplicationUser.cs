using Microsoft.AspNetCore.Identity;
namespace BlazorChatApp.Shared.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Avatar Avatar { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
