using System.Collections.Generic;
using doanwebnangcao.Models;

namespace doanwebnangcao.Controllers
{
    public class ChatViewModel
    {
        public List<User> Users { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public int? SelectedUserId { get; set; }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int UnreadMessagesCount { get; set; }
    }
}