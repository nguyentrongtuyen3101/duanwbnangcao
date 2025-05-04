using doanwebnangcao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace doanwebnangcao.Controllers
{
    public class quanlythanhtoanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public quanlythanhtoanController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: quanlythanhtoan/thanhtoanmanager
        public ActionResult thanhtoanmanager(int page = 1)
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với quyền Admin để truy cập trang này.";
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy tất cả phương thức thanh toán để kiểm tra VNP
            var paymentMethods = _context.PaymentMethods.ToList();
            var vnpPaymentMethod = paymentMethods.FirstOrDefault(pm => pm.Name.ToLower().Contains("vnp"));

            // Lấy tất cả đơn hàng
            var orders = _context.Orders.ToList();

            foreach (var order in orders)
            {
                // Kiểm tra xem đơn hàng đã có trong bảng Payment chưa
                if (!_context.Payments.Any(p => p.OrderId == order.Id))
                {
                    bool shouldInsert = false;
                    string paymentStatus = "Chưa thanh toán";

                    // Nếu phương thức thanh toán là VNP
                    if (vnpPaymentMethod != null && order.PaymentMethodId == vnpPaymentMethod.Id)
                    {
                        shouldInsert = true;
                        paymentStatus = "Đã thanh toán";
                    }
                    // Nếu không phải VNP, chỉ insert khi trạng thái là Delivered
                    else if (order.Status == OrderStatus.Delivered)
                    {
                        shouldInsert = true;
                        paymentStatus = "Chưa thanh toán";
                    }

                    if (shouldInsert)
                    {
                        var payment = new Payment
                        {
                            OrderId = order.Id,
                            PaymentMethodId = order.PaymentMethodId,
                            Amount = order.TotalAmount,
                            PaymentDate = DateTime.Now,
                            Status = paymentStatus,
                            TransactionId = null // Có thể cập nhật sau nếu có giao dịch
                        };
                        _context.Payments.Add(payment);
                    }
                }
            }
            _context.SaveChanges();

            // Phân trang
            const int pageSize = 10;
            var totalRecords = _context.Payments.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var payments = _context.Payments.OrderByDescending(p => p.PaymentDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(payments);
        }

        // POST: quanlythanhtoan/UpdatePaymentStatus
        [HttpPost]
        public ActionResult UpdatePaymentStatus(int paymentId, bool isPaid)
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập với quyền Admin để thực hiện hành động này." });
            }

            var payment = _context.Payments.Find(paymentId);
            if (payment == null)
            {
                return Json(new { success = false, message = "Thanh toán không tồn tại." });
            }

            payment.Status = isPaid ? "Đã thanh toán" : "Chưa thanh toán";
            _context.SaveChanges();

            return Json(new { success = true, message = "Cập nhật trạng thái thanh toán thành công!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}