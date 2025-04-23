using doanwebnangcao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace doanwebnangcao.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ThanhToanAdminController : Controller
    {
        // GET: ThanhToanAdmin
        private readonly ApplicationDbContext _context;
        public ThanhToanAdminController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: ThanhToanAdmin/thanhtoanadmin
        public ActionResult thanhtoanadmin()
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với quyền Admin để truy cập trang này.";
                return RedirectToAction("DangNhap", "Home");
            }

            var paymentMethods = _context.PaymentMethods.ToList();
            return View(paymentMethods);
        }

        // POST: ThanhToanAdmin/CreatePaymentMethod
        [HttpPost]
        public ActionResult CreatePaymentMethod(PaymentMethod paymentMethod)
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với quyền Admin để thực hiện hành động này.";
                return RedirectToAction("DangNhap", "Home");
            }

            if (!ModelState.IsValid)
            {
                var paymentMethods = _context.PaymentMethods.ToList();
                return View("thanhtoanadmin", paymentMethods);
            }

            // Kiểm tra tên hình thức thanh toán đã tồn tại
            if (_context.PaymentMethods.Any(pm => pm.Name == paymentMethod.Name))
            {
                TempData["ErrorMessage"] = "Tên hình thức thanh toán đã tồn tại.";
                var paymentMethods = _context.PaymentMethods.ToList();
                return View("thanhtoanadmin", paymentMethods);
            }

            paymentMethod.IsActive = true; // Mặc định trạng thái là hoạt động
            _context.PaymentMethods.Add(paymentMethod);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm hình thức thanh toán thành công!";
            return RedirectToAction("thanhtoanadmin");
        }

        // POST: ThanhToanAdmin/EditPaymentMethod
        [HttpPost]
        public ActionResult EditPaymentMethod(PaymentMethod paymentMethod)
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với quyền Admin để thực hiện hành động này.";
                return RedirectToAction("DangNhap", "Home");
            }

            if (!ModelState.IsValid)
            {
                var paymentMethods = _context.PaymentMethods.ToList();
                return View("thanhtoanadmin", paymentMethods);
            }

            var existingPaymentMethod = _context.PaymentMethods.Find(paymentMethod.Id);
            if (existingPaymentMethod == null)
            {
                TempData["ErrorMessage"] = "Hình thức thanh toán không tồn tại.";
                return RedirectToAction("thanhtoanadmin");
            }

            // Kiểm tra tên trùng lặp (trừ chính nó)
            if (_context.PaymentMethods.Any(pm => pm.Name == paymentMethod.Name && pm.Id != paymentMethod.Id))
            {
                TempData["ErrorMessage"] = "Tên hình thức thanh toán đã tồn tại.";
                var paymentMethods = _context.PaymentMethods.ToList();
                return View("thanhtoanadmin", paymentMethods);
            }

            existingPaymentMethod.Name = paymentMethod.Name;
            existingPaymentMethod.Description = paymentMethod.Description;
            existingPaymentMethod.IsActive = paymentMethod.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật hình thức thanh toán thành công!";
            return RedirectToAction("thanhtoanadmin");
        }

        // GET: ThanhToanAdmin/DeletePaymentMethod
        public ActionResult DeletePaymentMethod(int id)
        {
            // Kiểm tra session
            if (Session["UserId"] == null || Session["Role"]?.ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với quyền Admin để thực hiện hành động này.";
                return RedirectToAction("DangNhap", "Home");
            }

            var paymentMethod = _context.PaymentMethods.Find(id);
            if (paymentMethod == null)
            {
                TempData["ErrorMessage"] = "Hình thức thanh toán không tồn tại.";
                return RedirectToAction("thanhtoanadmin");
            }

            // Kiểm tra xem có đơn hàng hoặc thanh toán liên quan không
            if (_context.Orders.Any(o => o.PaymentMethodId == id) || _context.Payments.Any(p => p.PaymentMethodId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa hình thức thanh toán vì vẫn còn đơn hàng hoặc thanh toán liên quan.";
                return RedirectToAction("thanhtoanadmin");
            }

            _context.PaymentMethods.Remove(paymentMethod);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa hình thức thanh toán thành công!";
            return RedirectToAction("thanhtoanadmin");
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
