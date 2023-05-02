using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorChatApp.Shared.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        [Required]
        public string SenderUserId { get; set; }
        [Required]
        public string ReceiverUserId { get; set; }
        [Required]
        public string Message { get; set; }
        public bool ReadByReceiver { get; set; }
        public DateTime SendDate { get; set; }
        public bool IsImgMessage { get; set; }
        [NotMapped]
        public byte[] ImgBytes { get; set; }
        [NotMapped]
        public string ImgFileExtension { get; set; }
        public string ImgFileName { get; set; }
    }
}
