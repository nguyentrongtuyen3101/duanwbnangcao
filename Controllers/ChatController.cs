using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;
using Microsoft.AspNet.SignalR;
using doanwebnangcao.Hubs;

namespace doanwebnangcao.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Chat/Chatbox
        public ActionResult Chatbox(int? userId)
        {
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = _context.Users.Find(currentUserId);
            if (currentUser == null)
            {
                return HttpNotFound("User not found.");
            }

            var model = new ChatViewModel
            {
                Users = new List<User>(),
                Messages = new List<ChatMessage>(),
                SelectedUserId = userId
            };

            // Xác định vai trò của người dùng
            ViewBag.Role = currentUser.Role.ToString();

            // Nếu là Admin, lấy danh sách tất cả người dùng (trừ Admin)
            if (currentUser.Role == Role.Admin)
            {
                var users = _context.Users
                    .Where(u => u.Role == Role.User && u.IsActive)
                    .Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        UnreadMessagesCount = u.ReceivedMessages
                            .Count(m => m.ReceiverId == u.Id && m.SenderId == currentUserId && !m.IsRead)
                    })
                    .ToList();

                model.Users = _context.Users
                    .Where(u => u.Role == Role.User && u.IsActive)
                    .ToList();

                // Nếu Admin chọn một người dùng để chat, lấy tin nhắn
                if (userId.HasValue)
                {
                    model.Messages = _context.ChatMessages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                        .OrderBy(m => m.SentAt)
                        .ToList();
                }
            }
            else // Người dùng thường
            {
                // Người dùng thường chỉ chat với Admin (giả định Admin có Id = 1)
                var adminId = 1; // Cần thay đổi nếu AdminId khác
                model.Messages = _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == adminId) ||
                                (m.SenderId == adminId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToList();
            }

            return View(model);
        }

        [HttpPost]
        public JsonResult UploadFile(int receiverId)
        {
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var file = Request.Files[0];
            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "No file uploaded." });
            }

            try
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/Uploads/ChatFiles/"), fileName);
                file.SaveAs(path);

                var filePath = $"/Uploads/ChatFiles/{fileName}";
                var message = new ChatMessage
                {
                    SenderId = (int)currentUserId,
                    ReceiverId = receiverId,
                    Content = fileName,
                    FilePath = filePath,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.ChatMessages.Add(message);
                _context.SaveChanges();

                // Gửi tin nhắn qua SignalR
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                hubContext.Clients.All.receiveMessage(new
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    FilePath = message.FilePath,
                    SentAt = message.SentAt.ToString("o"),
                    IsRead = message.IsRead
                });

                return Json(new { success = true, filePath = filePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}