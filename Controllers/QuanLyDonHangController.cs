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

                    // Kiểm tra trạng thái hiện tại của đơn hàng
                    if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Delivered)
                    {
                        return Json(new { success = false, message = "Đơn hàng đã ở trạng thái cuối (Đã giao hoặc Đã hủy), không thể cập nhật.", page });
                    }

                    // Lấy trạng thái mới
                    var newStatus = order.Status + 1;

                    // Chỉ xử lý trừ tồn kho nếu trạng thái mới là Confirmed
                    if (newStatus == OrderStatus.Confirmed)
                    {
                        // Thu thập ProductId duy nhất
                        var productIds = new HashSet<int>();

                        // Cập nhật ProductVariant
                        foreach (var detail in order.OrderDetails)
                        {
                            var productVariant = detail.ProductVariant;
                            if (productVariant == null)
                            {
                                return Json(new { success = false, message = "Biến thể sản phẩm không tồn tại.", page });
                            }

                            // Kiểm tra số lượng tồn kho
                            if (productVariant.StockQuantity < detail.Quantity)
                            {
                                return Json(new { success = false, message = $"Sản phẩm {productVariant.ProductId} (biến thể) không đủ số lượng tồn kho.", page });
                            }

                            // Trừ số lượng tồn kho của biến thể
                            productVariant.StockQuantity -= detail.Quantity;
                            _context.Entry(productVariant).State = EntityState.Modified;

                            // Thu thập ProductId
                            productIds.Add(productVariant.ProductId);
                        }

                        // Lưu thay đổi cho ProductVariant
                        _context.SaveChanges();

                        // Cập nhật Product bằng SQL trực tiếp
                        foreach (var productId in productIds)
                        {
                            var totalVariantStock = _context.ProductVariants
                                .Where(pv => pv.ProductId == productId && pv.IsActive)
                                .Sum(pv => pv.StockQuantity);

                            _context.Database.ExecuteSqlCommand(
                                "UPDATE Products SET StockQuantity = @p0 WHERE Id = @p1",
                                totalVariantStock, productId);
                        }
                    }

                    // Cập nhật trạng thái đơn hàng
                    order.Status = newStatus;
                    _context.Entry(order).State = EntityState.Modified;
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công!", newStatus = order.Status.ToString(), page });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
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

                    // Hoàn lại số lượng tồn kho nếu trạng thái chưa phải là Delivered
                    if (order.Status != OrderStatus.Delivered)
                    {
                        // Thu thập ProductId duy nhất
                        var productIds = new HashSet<int>();

                        // Cập nhật ProductVariant
                        foreach (var detail in order.OrderDetails)
                        {
                            var productVariant = detail.ProductVariant;
                            if (productVariant != null)
                            {
                                // Hoàn lại số lượng tồn kho cho biến thể
                                productVariant.StockQuantity += detail.Quantity;
                                _context.Entry(productVariant).State = EntityState.Modified;

                                // Thu thập ProductId
                                productIds.Add(productVariant.ProductId);
                            }
                        }

                        // Lưu thay đổi cho ProductVariant
                        _context.SaveChanges();

                        // Cập nhật Product bằng SQL trực tiếp
                        foreach (var productId in productIds)
                        {
                            var totalVariantStock = _context.ProductVariants
                                .Where(pv => pv.ProductId == productId && pv.IsActive)
                                .Sum(pv => pv.StockQuantity);

                            _context.Database.ExecuteSqlCommand(
                                "UPDATE Products SET StockQuantity = @p0 WHERE Id = @p1",
                                totalVariantStock, productId);
                        }
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

                    // Remove the order
                    _context.Orders.Remove(order);
                    _context.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Xóa đơn hàng thành công!", page });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message, page });
                }
            }
        }
    }
}