using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using doanwebnangcao.Models;
using System.Data.Entity;
using System.Net.Mail;
using System.Net;

namespace doanwebnangcao.Controllers
{
    public class KhuyenMaiCodeController : Controller
    {
        private ApplicationDbContext _context;

        public KhuyenMaiCodeController()
        {
            _context = new ApplicationDbContext();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        // GET: KhuyenMaiCode
        public ActionResult khuyenmaimanager()
        {
            var coupons = _context.Coupons.ToList();
            ViewBag.RandomCode = GenerateRandomCode();
            return View("khuyenmaimanager", coupons);
        }

        // POST: CreateCoupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCoupon(Coupon model, string DiscountType, int UsageLimitDays)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã giảm giá đã tồn tại
                if (_context.Coupons.Any(c => c.Code == model.Code))
                {
                    TempData["ErrorMessage"] = "Mã giảm giá đã tồn tại. Vui lòng thử lại.";
                    return RedirectToAction("khuyenmaimanager");
                }

                // Xử lý loại giảm giá
                if (DiscountType == "percentage")
                {
                    model.DiscountAmount = null;
                }
                else
                {
                    model.DiscountPercentage = null;
                }

                // Tính ngày kết thúc
                model.StartDate = DateTime.Today;
                model.EndDate = model.StartDate.AddDays(UsageLimitDays);

                // Đảm bảo UsedCount mặc định là 0
                model.UsedCount = 0;

                _context.Coupons.Add(model);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Thêm mã giảm giá thành công!";
                return RedirectToAction("khuyenmaimanager");
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction("khuyenmaimanager");
        }

        // POST: EditCoupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCoupon(Coupon model, string DiscountType, int UsageLimitDays)
        {
            if (ModelState.IsValid)
            {
                var coupon = _context.Coupons.Find(model.Id);
                if (coupon == null)
                {
                    TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
                    return RedirectToAction("khuyenmaimanager");
                }

                // Cập nhật thông tin
                coupon.Code = model.Code; // Mã không thay đổi, chỉ để đảm bảo
                if (DiscountType == "percentage")
                {
                    coupon.DiscountPercentage = model.DiscountPercentage;
                    coupon.DiscountAmount = null;
                }
                else
                {
                    coupon.DiscountAmount = model.DiscountAmount;
                    coupon.DiscountPercentage = null;
                }
                coupon.StartDate = model.StartDate;
                coupon.EndDate = model.StartDate.AddDays(UsageLimitDays);
                coupon.MaxUsage = model.MaxUsage;
                coupon.IsActive = model.IsActive;

                _context.Entry(coupon).State = EntityState.Modified;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật mã giảm giá thành công!";
                return RedirectToAction("khuyenmaimanager");
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction("khuyenmaimanager");
        }

        // GET: DeleteCoupon
        public ActionResult DeleteCoupon(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
                return RedirectToAction("khuyenmaimanager");
            }

            _context.Coupons.Remove(coupon);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa mã giảm giá thành công!";
            return RedirectToAction("khuyenmaimanager");
        }

        // GET: ShowGiftModal
        [HttpGet]
        public ActionResult ShowGiftModal(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." }, JsonRequestBehavior.AllowGet);
            }

            var users = _context.Users
                .Where(u => u.IsActive && u.Role != Role.Admin)
                .Select(u => new { id = u.Id, email = u.Email })
                .ToList();
            if (users == null || !users.Any())
            {
                return Json(new { success = false, message = "Không có người dùng nào để tặng." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = users }, JsonRequestBehavior.AllowGet);
        }

        // POST: GiftCouponToUser
        [HttpPost]
        public ActionResult GiftCouponToUser(int couponId, int userId)
        {
            var coupon = _context.Coupons.Find(couponId);
            var user = _context.Users.Find(userId);

            if (coupon == null || user == null)
            {
                TempData["ErrorMessage"] = "Mã giảm giá hoặc người dùng không tồn tại.";
                return RedirectToAction("khuyenmaimanager");
            }

            try
            {
                string subject = "Bạn được tặng mã giảm giá";
                string body = $"Bạn được tặng mã giảm giá #{coupon.Code}. " +
                              $"Bấm vào <a href='https://localhost:44377/HangMoiVe/hangmoive'>Đây</a> để mua hàng ngay với mã giảm giá.";
                SendEmail(user.Email, subject, body);

                TempData["SuccessMessage"] = $"Đã gửi mã giảm giá #{coupon.Code} đến email {user.Email} thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi email: {ex.Message}";
            }

            return RedirectToAction("khuyenmaimanager");
        }

        // POST: GiftCoupon (Placeholder - không cần nữa nếu dùng GiftCouponToUser)
        [HttpPost]
        public ActionResult GiftCoupon(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Mã giảm giá không tồn tại.";
                return RedirectToAction("khuyenmaimanager");
            }

            TempData["SuccessMessage"] = $"Chức năng tặng mã giảm giá (ID: {id}) đang được phát triển.";
            return RedirectToAction("khuyenmaimanager");
        }

        // Phương thức mới để tăng UsedCount
        [HttpPost]
        public JsonResult IncrementUsedCount(string couponCode)
        {
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            }

            if (!coupon.IsActive || coupon.EndDate < DateTime.Today || (coupon.MaxUsage > 0 && coupon.UsedCount >= coupon.MaxUsage))
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            coupon.UsedCount += 1;
            _context.Entry(coupon).State = EntityState.Modified;
            _context.SaveChanges();

            return Json(new { success = true, message = "Đã tăng số lần sử dụng mã giảm giá." });
        }

        // Hàm gửi email
        private void SendEmail(string toEmail, string subject, string body)
        {
            var fromEmail = "tinhluc2@gmail.com";
            var fromPassword = "axqnsafslczyhcuy";

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true, // Cho phép sử dụng HTML trong body
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }

        // Hàm tạo mã ngẫu nhiên
        private string GenerateRandomCode()
        {
            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var code = "#";
            for (int i = 0; i < 6; i++)
            {
                code += characters[random.Next(characters.Length)];
            }
            return code;
        }
    }
}