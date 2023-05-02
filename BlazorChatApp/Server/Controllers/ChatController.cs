using BlazorChatApp.Server.Data;
using BlazorChatApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Security.Claims;

namespace BlazorChatApp.Server.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        
        public ChatController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string CurrentUserId
        {
            get
            {
                return User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
        }

        [HttpGet]

        public async Task<List<ChatMessage>> GetChatHistory(string currentUserId, string selectedUserId)
        {
            var result = await _dbContext.ChatMessage.AsNoTracking().Where(x => (x.SenderUserId == currentUserId && x.ReceiverUserId == selectedUserId) || (x.SenderUserId == selectedUserId && x.ReceiverUserId == currentUserId)).ToListAsync();
            result = result.OrderBy(x => x.SendDate).ToList();
            return result;
        }

        [HttpGet]
        public Dictionary<string, int> GetUnreadMessages()
        {
            Dictionary<string, int> unreadCountDict = new Dictionary<string, int>();
            var unreadMessages = _dbContext.ChatMessage.Where(x => !x.ReadByReceiver && x.ReceiverUserId == CurrentUserId);

            foreach (var um in unreadMessages)
            {
                if (!unreadCountDict.Keys.Contains(um.SenderUserId))
                {
                    unreadCountDict.Add(um.SenderUserId, 1);
                }
                else
                {
                    unreadCountDict[um.SenderUserId] += 1;
                }
            }

            if (unreadMessages.Any())
            {
                return unreadCountDict;
            }
            else
            {
                return new Dictionary<string, int>();
            }
        }
    }
}