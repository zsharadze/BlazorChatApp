using BlazorChatApp.Client.Pages.Account;
using BlazorChatApp.Server.Infrastructure;
using BlazorChatApp.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlazorChatApp.Server.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailValidator _emailValidator;
        private readonly IConfiguration _configuration;

        public AuthenticationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, EmailValidator emailValidator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailValidator = emailValidator;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                if (!_emailValidator.IsValidEmail(model.Email))
                {
                    return Ok(new RegisterResult { Success = false, Message = "Invalid email address" });
                }

                var userEmailExists = await _userManager.FindByEmailAsync(model.Email);
                if (userEmailExists != null)
                    return Ok(new RegisterResult { Success = false, Message = "User already exists!" });

                var userExists = await _userManager.FindByNameAsync(model.Username);
                if (userExists != null)
                    return Ok(new RegisterResult { Success = false, Message = "User already exists!" });

                ApplicationUser user = new ApplicationUser()
                {
                    Email = model.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Username,
                    RegistrationDate = DateTime.Now,
                    Avatar = model.Avatar
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                    return StatusCode(StatusCodes.Status500InternalServerError, new RegisterResult { Success = false, Message = "User creation failed! Please check user details and try again." });
                return Ok(new RegisterResult { Success = true, Message = "User created successfully!" });
            }
            else
            {
                return Ok(new RegisterResult { Success = false, Message = "Invalid Model!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                //use this for email login
                //var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    //use this for email login
                    //var claims = new[] { new Claim(type: ClaimTypes.Name, model.Email) };
                    var claims = new[] { new Claim(type: ClaimTypes.Name, model.Username) };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    DateTime expiry;
                    expiry = DateTime.Now.AddDays(30);

                    var token = new JwtSecurityToken(
                        _configuration["JwtIssuer"],
                        _configuration["JwtAudience"],
                        claims,
                        expires: expiry,
                        signingCredentials: creds
                    );

                    var user = await _userManager.FindByNameAsync(model.Username);
                    return Ok(new LoginResult { Success = true, UserId = user.Id, Token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
                if (result.IsLockedOut)
                {
                    return Ok(new LoginResult { Success = false, Message = "Lockout" });
                }
                else
                {
                    return Ok(new LoginResult { Success = false, Message = "Invalid login attempt." });
                }
            }
            else
            {
                return Ok(new LoginResult { Success = false, Message = "Invalid Model!" });
            }
        }
    }
}
