using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanwebnangcao.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string Content { get; set; }

        public string FilePath { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; }
    }
}