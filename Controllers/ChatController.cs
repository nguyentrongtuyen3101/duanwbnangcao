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
        public ActionResult Chatbox(int? userId, int page = 1)
        {
            const int messagesPerPage = 12; // Số lượng tin nhắn mỗi trang
            const int usersPerPage = 10;    // Số lượng người dùng mỗi trang

            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                return RedirectToAction("DangNhap", "Home");
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

            // Nếu là Admin, lấy danh sách tất cả người dùng (trừ Admin) với phân trang
            if (currentUser.Role == Role.Admin)
            {
                var users = _context.Users
                    .Where(u => u.Role == Role.User && u.IsActive)
                    .OrderBy(u => u.Id)
                    .Skip((page - 1) * usersPerPage)
                    .Take(usersPerPage)
                    .ToList();

                // Tính số lượng tin nhắn chưa đọc cho từng người dùng và lưu vào ViewBag
                var userUnreadCounts = users.ToDictionary(
                    u => u.Id,
                    u => _context.ChatMessages.Count(m => m.ReceiverId == currentUserId && m.SenderId == u.Id && !m.IsRead)
                );
                ViewBag.UserUnreadCounts = userUnreadCounts;

                model.Users = users;

                // Nếu Admin chọn một người dùng để chat, lấy tin nhắn với phân trang
                if (userId.HasValue)
                {
                    model.Messages = _context.ChatMessages
                        .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                        .OrderByDescending(m => m.SentAt)
                        .Skip((page - 1) * messagesPerPage)
                        .Take(messagesPerPage)
                        .OrderBy(m => m.SentAt) // Sắp xếp lại theo thứ tự thời gian tăng dần để hiển thị
                        .ToList();
                }
            }
            else // Người dùng thường
            {
                // Người dùng thường chỉ chat với Admin (giả định Admin có Id = 1)
                var adminId = 1; // Cần thay đổi nếu AdminId khác
                var admin = _context.Users.Find(adminId);
                if (admin != null)
                {
                    var unreadCount = _context.ChatMessages
                        .Count(m => m.ReceiverId == currentUserId && m.SenderId == adminId && !m.IsRead);
                    ViewBag.UserUnreadCounts = new Dictionary<int, int> { { adminId, unreadCount } };
                    model.Users.Add(admin);
                }

                model.Messages = _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == adminId) ||
                                (m.SenderId == adminId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * messagesPerPage)
                    .Take(messagesPerPage)
                    .OrderBy(m => m.SentAt) // Sắp xếp lại theo thứ tự thời gian tăng dần để hiển thị
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
                return Json(new { success = false, message = "Người dùng chưa đăng nhập." });
            }

            var file = Request.Files[0];
            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "Không có file nào được tải lên." });
            }

            try
            {
                // Đảm bảo thư mục tồn tại
                var uploadDir = Server.MapPath("~/Uploads/ChatFiles");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var fileName = Path.GetFileName(file.FileName);
                // Đảm bảo tên file không chứa ký tự không hợp lệ
                fileName = Path.GetFileNameWithoutExtension(fileName) + "_" + DateTime.Now.Ticks + Path.GetExtension(fileName);
                var path = Path.Combine(uploadDir, fileName);

                // Lưu file
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

                // Gửi tin nhắn qua SignalR cho cả người nhận và người gửi
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
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

                // Gửi tới người nhận và người gửi
                hubContext.Clients.Users(new[] { receiverId.ToString(), currentUserId.ToString() }).receiveMessage(messageData);

                // Cập nhật số lượng tin nhắn chưa đọc cho người nhận
                var unreadCount = _context.ChatMessages
                    .Count(m => m.ReceiverId == receiverId && !m.IsRead);
                hubContext.Clients.User(receiverId.ToString()).updateUnreadMessagesCountForHeader(unreadCount);

                return Json(new { success = true, filePath = filePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lưu file: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult LoadMoreMessages(int userId, int page)
        {
            const int messagesPerPage = 12;
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Người dùng chưa đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            var messages = new List<ChatMessage>();
            var role = ViewBag.Role?.ToString() ?? Session["Role"]?.ToString();

            if (role == "Admin")
            {
                messages = _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * messagesPerPage)
                    .Take(messagesPerPage)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                // Đánh dấu tin nhắn là đã đọc
                var messagesToMarkAsRead = _context.ChatMessages
                    .Where(m => m.SenderId == userId && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToList();

                foreach (var message in messagesToMarkAsRead)
                {
                    message.IsRead = true;
                }
                _context.SaveChanges();

                // Cập nhật số lượng tin nhắn chưa đọc qua SignalR
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                int unreadCount = _context.ChatMessages
                    .Count(m => m.ReceiverId == currentUserId && !m.IsRead);
                hubContext.Clients.User(currentUserId.ToString()).updateUnreadMessagesCountForHeader(unreadCount);
            }
            else
            {
                var adminId = 1; // Giả định AdminId = 1
                messages = _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == adminId) ||
                                (m.SenderId == adminId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * messagesPerPage)
                    .Take(messagesPerPage)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                // Đánh dấu tin nhắn là đã đọc
                var messagesToMarkAsRead = _context.ChatMessages
                    .Where(m => m.SenderId == adminId && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToList();

                foreach (var message in messagesToMarkAsRead)
                {
                    message.IsRead = true;
                }
                _context.SaveChanges();

                // Cập nhật số lượng tin nhắn chưa đọc qua SignalR
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                int unreadCount = _context.ChatMessages
                    .Count(m => m.ReceiverId == currentUserId && !m.IsRead);
                hubContext.Clients.User(currentUserId.ToString()).updateUnreadMessagesCountForHeader(unreadCount);
            }

            // Kiểm tra nếu không còn tin nhắn để tải
            var hasMoreMessages = messages.Count == messagesPerPage;

            var messageList = messages.Select(m => new
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                FilePath = m.FilePath,
                SentAt = m.SentAt.ToString("HH:mm"),
                IsRead = m.IsRead
            }).ToList();

            return Json(new { success = true, messages = messageList, hasMoreMessages = hasMoreMessages }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LoadMoreUsers(int page)
        {
            const int usersPerPage = 10;
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null || (ViewBag.Role?.ToString() ?? Session["Role"]?.ToString()) != "Admin")
            {
                return Json(new { success = false, message = "Không có quyền truy cập." }, JsonRequestBehavior.AllowGet);
            }

            var users = _context.Users
                .Where(u => u.Role == Role.User && u.IsActive)
                .OrderBy(u => u.Id)
                .Skip((page - 1) * usersPerPage)
                .Take(usersPerPage)
                .ToList();

            var hasMoreUsers = users.Count == usersPerPage;

            var userList = users.Select(u => new
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                Email = u.Email,
                UnreadMessagesCount = _context.ChatMessages
                    .Count(m => m.ReceiverId == currentUserId && m.SenderId == u.Id && !m.IsRead)
            }).ToList();

            return Json(new { success = true, users = userList, hasMoreUsers = hasMoreUsers }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUnreadMessagesCount(int userId)
        {
            if (userId <= 0)
            {
                return Json(new { success = false, message = "Invalid user ID" }, JsonRequestBehavior.AllowGet);
            }

            int count = _context.ChatMessages
                .Count(m => m.ReceiverId == userId && !m.IsRead);

            return Json(new { success = true, count = count }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}