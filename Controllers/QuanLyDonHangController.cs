using doanwebnangcao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace doanwebnangcao.Controllers
{
    public class QuanLyDonHangController : Controller
    {
        private readonly ApplicationDbContext _context;
        // GET: QuanLyDonHang
        public QuanLyDonHangController()
        {
            _context = new ApplicationDbContext();
        }

        // Check session validity
        private bool IsSessionValid()
        {
            return HttpContext.Session["UserId"] != null && HttpContext.Session["Role"]?.ToString() == "Admin";
        }

        // GET: QuanLyDonHang
        public ActionResult quanlydonhang(int page = 1, int pageSize = 10)
        {
            if (!IsSessionValid())
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("DangNhap", "Home");
            }

            ViewBag.ActivePage = "QuanLyDonHang";

            // Fetch total number of orders
            var totalRecords = _context.Orders.Count();

            // Calculate pagination
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            // Fetch orders with related data, ordered by OrderDate descending
            var orders = _context.Orders
            .Include("User")
            .Include("ShippingAddress")
            .Include("PaymentMethod")
            .Include("OrderDetails.ProductVariant.ProductImages")
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

            // Pass pagination info to the view
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            return View(orders);
        }

        // POST: QuanLyDonHang/UpdateOrderStatus
        [HttpPost]
        public JsonResult UpdateOrderStatus(int orderId, int page = 1)
        {
            if (!IsSessionValid())
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", page });
            }

            try
            {
                var order = _context.Orders.Find(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại.", page });
                }

                // Check if the order can progress to the next status
                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                {
                    return Json(new { success = false, message = "Đơn hàng đã ở trạng thái cuối (Đã giao hoặc Đã hủy), không thể cập nhật.", page });
                }

                // Progress the status to the next step
                order.Status = order.Status + 1; // Enum values are sequential: Pending -> Confirmed -> Processing -> Shipped -> Delivered

                _context.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công!", newStatus = order.Status.ToString(), page });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message, page });
            }
        }

        // GET: QuanLyDonHang/GetOrderDetails
        [HttpGet]
        public JsonResult GetOrderDetails(int orderId)
        {
            if (!IsSessionValid())
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var orderDetails = _context.OrderDetails
                 .Include("ProductVariant")
                 .Include("ProductVariant.Product")
                 .Include("ProductVariant.ProductImages")
                 .Where(od => od.OrderId == orderId)
                 .Select(od => new
                 {
                     od.Id,
                     ProductName = od.ProductVariant.Product.Name,
                     VariantImageUrl = od.ProductVariant.ProductImages != null && od.ProductVariant.ProductImages.Any(pi => pi.IsMain)
                         ? od.ProductVariant.ProductImages.First(pi => pi.IsMain).ImageUrl
                         : od.ProductVariant.VariantImageUrl,
                     od.Quantity,
                     od.UnitPrice,
                     od.Subtotal
                 })
                 .ToList();

                return Json(new { success = true, orderDetails }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy chi tiết đơn hàng: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: QuanLyDonHang/DeleteOrder
        [HttpPost]
        public JsonResult DeleteOrder(int orderId, int page = 1)
        {
            if (!IsSessionValid())
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", page });
            }

            try
            {
                var order = _context.Orders
                .Include("OrderDetails")
                .Include("Payments")
                .SingleOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại.", page });
                }

                // Remove related OrderDetails and Payments
                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails);
                }

                if (order.Payments != null && order.Payments.Any())
                {
                    _context.Payments.RemoveRange(order.Payments);
                }

                _context.Orders.Remove(order);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa đơn hàng thành công!", page });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message, page });
            }
        }
    }
}
