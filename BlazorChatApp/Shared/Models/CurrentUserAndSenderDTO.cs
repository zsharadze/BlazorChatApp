using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorChatApp.Shared.Models
{
    public class CurrentUserAndSenderDTO
    {
        public string CurrentUserId { get; set; }
        public string SelectedUserId { get; set; }
    }
}
