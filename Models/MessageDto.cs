using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace doanwebnangcao.Models
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; }
        public string FilePath { get; set; }
        public string SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}