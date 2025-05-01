using doanwebnangcao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace doanwebnangcao.Controllers
{
    public class QuanLyDonHangController : Controller
    {
        private readonly ApplicationDbContext _context;

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

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var order = _context.Orders
                        .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                        .FirstOrDefault(o => o.Id == orderId);

                    if (order == null)
                    {
                        return Json(new { success = false, message = "Đơn hàng không tồn tại.", page });
                    }

                    if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                    {
                        return Json(new { success = false, message = "Đơn hàng đã ở trạng thái cuối (Đã giao hoặc Đã hủy), không thể cập nhật.", page });
                    }

                    var newStatus = order.Status + 1;

                    // Trừ tồn kho khi trạng thái chuyển sang Shipped
                    if (newStatus == OrderStatus.Shipped)
                    {
                        var productIds = new HashSet<int>();

                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.ProductVariant == null)
                            {
                                return Json(new { success = false, message = "Biến thể sản phẩm không tồn tại.", page });
                            }

                            if (detail.ProductVariant.StockQuantity < detail.Quantity)
                            {
                                return Json(new { success = false, message = $"Sản phẩm {detail.ProductVariant.ProductId} (biến thể) không đủ số lượng tồn kho.", page });
                            }

                            detail.ProductVariant.StockQuantity -= detail.Quantity;
                            _context.Entry(detail.ProductVariant).State = EntityState.Modified;

                            productIds.Add(detail.ProductVariant.ProductId);
                        }

                        _context.SaveChanges();

                        foreach (var productId in productIds)
                        {
                            var totalVariantStock = _context.ProductVariants
                                .Where(pv => pv.ProductId == productId && pv.IsActive)
                                .Sum(pv => (int?)pv.StockQuantity) ?? 0;

                            _context.Database.ExecuteSqlCommand(
                                "UPDATE Products SET StockQuantity = @p0 WHERE Id = @p1",
                                totalVariantStock, productId);
                        }
                    }

                    order.Status = newStatus;
                    _context.Entry(order).State = EntityState.Modified;
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công!", newStatus = order.Status.ToString(), page });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Error in UpdateOrderStatus: {ex.Message}");
                    return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message, page });
                }
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
                // Truy vấn cơ bản để lấy dữ liệu từ cơ sở dữ liệu
                var orderDetails = _context.OrderDetails
                    .Include("ProductVariant")
                    .Include("ProductVariant.Product")
                    .Include("ProductVariant.ProductImages")
                    .Where(od => od.OrderId == orderId)
                    .Select(od => new
                    {
                        od.Id,
                        ProductName = od.ProductVariant.Product.Name,
                        ProductImages = od.ProductVariant.ProductImages, // Lấy danh sách ProductImages
                        VariantImageUrl = od.ProductVariant.VariantImageUrl, // Lấy VariantImageUrl mặc định
                        od.Quantity,
                        od.UnitPrice,
                        od.Subtotal
                    })
                    .ToList();

                // Xử lý logic ProductImages trong bộ nhớ
                var result = orderDetails.Select(od => new
                {
                    od.Id,
                    od.ProductName,
                    VariantImageUrl = od.ProductImages != null && od.ProductImages.Any(pi => pi.IsMain)
                        ? od.ProductImages.First(pi => pi.IsMain).ImageUrl
                        : od.VariantImageUrl,
                    od.Quantity,
                    od.UnitPrice,
                    od.Subtotal
                }).ToList();

                return Json(new { success = true, orderDetails = result }, JsonRequestBehavior.AllowGet);
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

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var order = _context.Orders
                        .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                        .Include("Payments")
                        .SingleOrDefault(o => o.Id == orderId);

                    if (order == null)
                    {
                        return Json(new { success = false, message = "Đơn hàng không tồn tại.", page });
                    }

                    // Hoàn lại tồn kho nếu trạng thái là Shipped hoặc Delivered (tồn kho đã bị trừ)
                    if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                    {
                        var productIds = new HashSet<int>();

                        foreach (var detail in order.OrderDetails)
                        {
                            var productVariant = detail.ProductVariant;
                            if (productVariant != null)
                            {
                                productVariant.StockQuantity += detail.Quantity;
                                _context.Entry(productVariant).State = EntityState.Modified;
                                productIds.Add(productVariant.ProductId);
                            }
                        }

                        _context.SaveChanges();

                        foreach (var productId in productIds)
                        {
                            var totalVariantStock = _context.ProductVariants
                                .Where(pv => pv.ProductId == productId && pv.IsActive)
                                .Sum(pv => (int?)pv.StockQuantity) ?? 0;

                            _context.Database.ExecuteSqlCommand(
                                "UPDATE Products SET StockQuantity = @p0 WHERE Id = @p1",
                                totalVariantStock, productId);
                        }
                    }

                    // Xóa OrderDetails và Payments
                    if (order.OrderDetails != null && order.OrderDetails.Any())
                    {
                        _context.OrderDetails.RemoveRange(order.OrderDetails);
                    }

                    if (order.Payments != null && order.Payments.Any())
                    {
                        _context.Payments.RemoveRange(order.Payments);
                    }

                    // Xóa đơn hàng
                    _context.Orders.Remove(order);
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Xóa đơn hàng thành công!", page });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Error in DeleteOrder: {ex.Message}");
                    return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message, page });
                }
            }
        }
    }
}