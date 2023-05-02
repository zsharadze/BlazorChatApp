using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlazorChatApp.Shared.Models
{
    public class ChatUser
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public Avatar Avatar { get; set; }
        public bool IsOnline { get; set; }
        public int UnreadMessageCount { get; set; }
    }
}
