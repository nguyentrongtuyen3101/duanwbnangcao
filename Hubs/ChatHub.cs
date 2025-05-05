using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using doanwebnangcao.Models;
using doanwebnangcao.Hubs;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;

namespace doanwebnangcao.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> Connections = new ConcurrentDictionary<string, string>();
        private readonly ApplicationDbContext _context;

        public ChatHub()
        {
            _context = new ApplicationDbContext();
        }

        public override Task OnConnected()
        {
            var userId = Context.QueryString["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                Connections.TryAdd(Context.ConnectionId, userId);
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userId;
            Connections.TryRemove(Context.ConnectionId, out userId);
            return base.OnDisconnected(stopCalled);
        }

        public void SendMessage(int senderId, int receiverId, string content, string filePath)
        {
            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                FilePath = filePath,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            _context.SaveChanges();

            var messageData = new
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                FilePath = message.FilePath,
                SentAt = message.SentAt.ToString("o"),
                IsRead = message.IsRead
            };

            Clients.All.receiveMessage(messageData);
        }

        public void MarkMessagesAsRead(int currentUserId, int otherUserId)
        {
            var messagesToMark = _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && m.SenderId == otherUserId && !m.IsRead)
                .ToList();

            foreach (var message in messagesToMark)
            {
                message.IsRead = true;
            }
            _context.SaveChanges();

            Clients.All.messagesRead(otherUserId);

            if (currentUserId != otherUserId) // Admin xem tin nhắn của user
            {
                var unreadCount = _context.ChatMessages
                    .Count(m => m.ReceiverId == otherUserId && !m.IsRead);
                Clients.All.updateUnreadMessagesCount(unreadCount);
            }
        }
    }
}