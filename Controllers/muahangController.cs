using doanwebnangcao.Models;
using doanwebnangcao.DTO;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Web;

namespace doanwebnangcao.Controllers
{
    public class muahangController : Controller
    {
        // Thông tin VNPay (được cập nhật từ email sandbox của VNPay)
        private const string VNPay_TmnCode = "3D8U719S"; // Terminal ID / Mã Website
        private const string VNPay_HashSecret = "14X61NZKV0USRUXHQPYYM2MSFZDHGNLJ"; // Secret Key / Chuỗi bí mật
        private const string VNPay_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"; // URL thanh toán môi trường TEST
        private const string VNPay_ReturnUrl = "http://localhost/muahang/VNPayPending"; // URL tạm thời (trang trung gian)
        // Lưu ý QUAN TRỌNG: VNPay không chấp nhận URL cục bộ (localhost). Bạn cần thay đổi VNPay_ReturnUrl thành URL công khai
        // Ví dụ: Sử dụng ngrok để tạo URL công khai tạm thời (https://your-ngrok-url.ngrok.io/muahang/VNPayReturn)
        // Sau đó, cung cấp URL này cho VNPay để cấu hình IPN (server-to-server) nhằm cập nhật trạng thái giao dịch.

        private readonly ApplicationDbContext _context;
        public muahangController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: muahang/dathang
        [HttpGet]
        public ActionResult dathang()
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];
            var latestAddress = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();
            var userAddresses = _context.Addresses
                .Where(a => a.UserId == userId)
                .ToList();

            var model = new OrderViewModel
            {
                Address = new Address()
            };

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = latestAddress;
            ViewBag.UserAddresses = userAddresses;
            return View(model);
        }

        // POST: muahang/dathang
        [HttpPost]
        public ActionResult dathang(int productId, int variantId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            if (productId <= 0 || variantId <= 0 || quantity <= 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("trangchu", "trangchu");
            }

            var product = _context.Products
                .Include(p => p.Subcategory)
                .SingleOrDefault(p => p.Id == productId && p.IsActive);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc không hoạt động.";
                return RedirectToAction("trangchu", "trangchu");
            }

            var variant = _context.ProductVariants
                .Include(v => v.Size)
                .Include(v => v.Color)
                .SingleOrDefault(v => v.Id == variantId && v.IsActive);

            if (variant == null)
            {
                TempData["ErrorMessage"] = "Biến thể sản phẩm không tồn tại hoặc không hoạt động.";
                return RedirectToAction("trangchu", "trangchu");
            }

            if (variant.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = "Số lượng không đủ.";
                return RedirectToAction("sanphamdetailt", "hangmoive", new { id = productId });
            }

            int userId = (int)Session["UserId"];
            var latestAddress = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();
            var userAddresses = _context.Addresses
                .Where(a => a.UserId == userId)
                .ToList();

            var model = new OrderViewModel
            {
                ProductId = productId,
                VariantId = variantId,
                Quantity = quantity,
                Product = product,
                Variant = variant,
                Address = new Address()
            };

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = latestAddress;
            ViewBag.UserAddresses = userAddresses;
            return View(model);
        }

        // POST: muahang/SaveAddressAndOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAddressAndOrder(OrderViewModel model)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            // Validate PhoneNumber
            if (!string.IsNullOrEmpty(model.Address.PhoneNumber) && !Regex.IsMatch(model.Address.PhoneNumber, @"^\d{10,15}$"))
            {
                ModelState.AddModelError("Address.PhoneNumber", "Số điện thoại phải từ 10-15 chữ số và chỉ chứa số.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                System.Diagnostics.Debug.WriteLine("ModelState Errors: " + string.Join(", ", errors));

                TempData["ErrorMessage"] = "Thông tin không hợp lệ. Vui lòng kiểm tra lại.";
                model.Product = _context.Products.SingleOrDefault(p => p.Id == model.ProductId);
                model.Variant = _context.ProductVariants.SingleOrDefault(v => v.Id == model.VariantId);
                ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
                ViewBag.SelectedAddress = _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault();
                ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
                return View("dathang", model);
            }

            var address = new Address
            {
                UserId = userId,
                FullName = model.Address.FullName,
                PhoneNumber = model.Address.PhoneNumber,
                AddressLine = model.Address.AddressLine,
                City = model.Address.City,
                Country = model.Address.Country
            };

            _context.Addresses.Add(address);
            _context.SaveChanges();

            model.Address.Id = address.Id;
            model.Product = _context.Products.SingleOrDefault(p => p.Id == model.ProductId);
            model.Variant = _context.ProductVariants.SingleOrDefault(v => v.Id == model.VariantId);

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = address;
            ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
            return View("dathang", model);
        }

        // POST: muahang/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(OrderViewModel model, int PaymentMethodId, bool? AcceptTerms)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            if (AcceptTerms != true)
            {
                TempData["ErrorMessage"] = "Vui lòng đồng ý với điều khoản dịch vụ.";
                model.Product = _context.Products.SingleOrDefault(p => p.Id == model.ProductId);
                model.Variant = _context.ProductVariants.SingleOrDefault(v => v.Id == model.VariantId);
                ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
                ViewBag.SelectedAddress = _context.Addresses
                    .Where(a => a.UserId == (int)Session["UserId"])
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault();
                ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == (int)Session["UserId"]).ToList();
                return View("dathang", model);
            }

            if (model.ProductId <= 0 || model.VariantId <= 0 || model.Quantity <= 0 || PaymentMethodId <= 0)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("dathang");
            }

            var product = _context.Products.SingleOrDefault(p => p.Id == model.ProductId);
            var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == model.VariantId);
            var paymentMethod = _context.PaymentMethods.SingleOrDefault(pm => pm.Id == PaymentMethodId);

            if (product == null || variant == null || paymentMethod == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm hoặc phương thức thanh toán không tồn tại.";
                return RedirectToAction("dathang");
            }

            if (variant.StockQuantity < model.Quantity)
            {
                TempData["ErrorMessage"] = "Số lượng không đủ.";
                return RedirectToAction("dathang");
            }

            int userId = (int)Session["UserId"];
            var address = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (address == null)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập địa chỉ giao hàng.";
                return RedirectToAction("dathang");
            }

            // Tạo đơn hàng
            var unitPrice = variant.VariantPrice ?? product.Price;
            var subtotal = unitPrice * model.Quantity;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = subtotal + 4, // Shipping cost mặc định là 4
                Status = OrderStatus.Pending, // Trạng thái mặc định là Pending
                ShippingAddressId = address.Id,
                ShippingCost = 4, // Shipping cost mặc định
                PaymentMethodId = PaymentMethodId,
                Notes = model.OrderNote
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                ProductVariantId = model.VariantId,
                Quantity = model.Quantity,
                UnitPrice = unitPrice,
                Subtotal = subtotal
            };

            _context.OrderDetails.Add(orderDetail);
            _context.SaveChanges();

            // Log để debug
            Debug.WriteLine($"PaymentMethodId: {PaymentMethodId}, PaymentMethod Name: {paymentMethod.Name}");

            // Kiểm tra loại phương thức thanh toán (đã cập nhật để khớp với dữ liệu trong bảng PaymentMethods)
            if (string.Equals(paymentMethod.Name, "Thanh toán qua VNP", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("Processing VNPay payment...");
                return ProcessVNPayPayment(order);
            }
            else if (string.Equals(paymentMethod.Name, "Thanh toán khi nhận hàng", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("Processing COD payment...");
                return ProcessCODPayment(order, variant);
            }
            else
            {
                // Xử lý mặc định: Vẫn cho phép xem đơn hàng
                Debug.WriteLine($"Phương thức thanh toán không được hỗ trợ: {paymentMethod.Name}");
                TempData["ErrorMessage"] = "Phương thức thanh toán không được hỗ trợ, nhưng đơn hàng đã được ghi nhận.";
                return RedirectToAction("quanlydonhang");
            }
        }

        // Xử lý thanh toán VNPay
        private ActionResult ProcessVNPayPayment(Order order)
        {
            try
            {
                string vnp_TxnRef = order.Id.ToString();
                string vnp_OrderInfo = $"Thanh toan don hang {order.Id}";
                long vnp_Amount = (long)(order.TotalAmount * 100);

                var vnpayParams = new Dictionary<string, string>
                {
                    { "vnp_Version", "2.1.0" },
                    { "vnp_Command", "pay" },
                    { "vnp_TmnCode", VNPay_TmnCode },
                    { "vnp_Amount", vnp_Amount.ToString() },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", "VND" },
                    { "vnp_IpAddr", GetClientIpAddress() },
                    { "vnp_Locale", "vn" },
                    { "vnp_OrderInfo", vnp_OrderInfo },
                    { "vnp_OrderType", "250000" },
                    { "vnp_ReturnUrl", VNPay_ReturnUrl + "?orderId=" + order.Id },
                    { "vnp_TxnRef", vnp_TxnRef },
                    { "vnp_SecureHashType", "SHA512" } // Thêm tham số SecureHashType để rõ ràng thuật toán mã hóa
                };

                // Log tất cả tham số trước khi tạo chữ ký
                Debug.WriteLine("VNPay Parameters:");
                foreach (var param in vnpayParams)
                {
                    Debug.WriteLine($"{param.Key}: {param.Value}");
                }

                // Sắp xếp tham số theo thứ tự từ điển
                var sortedParams = vnpayParams.OrderBy(kvp => kvp.Key).ToList();

                // Tạo chuỗi dữ liệu để tính chữ ký
                var signData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"));
                Debug.WriteLine($"SignData before hash: {signData}");

                // Tính chữ ký
                var vnp_SecureHash = HmacSHA512(VNPay_HashSecret, signData);
                Debug.WriteLine($"vnp_SecureHash: {vnp_SecureHash}");

                // Thêm chữ ký vào tham số
                vnpayParams.Remove("vnp_SecureHashType"); // Loại bỏ SecureHashType khỏi tham số gửi đi vì nó chỉ dùng để tạo chữ ký
                vnpayParams.Add("vnp_SecureHash", vnp_SecureHash);

                // Log lại tất cả tham số sau khi thêm chữ ký
                Debug.WriteLine("VNPay Parameters with SecureHash:");
                foreach (var param in vnpayParams)
                {
                    Debug.WriteLine($"{param.Key}: {param.Value}");
                }

                // Tạo URL thanh toán
                var paymentUrl = VNPay_Url + "?" + string.Join("&", vnpayParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"));

                // Log URL thanh toán để debug
                Debug.WriteLine($"VNPay Payment URL: {paymentUrl}");

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessVNPayPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán VNPay. Vui lòng thử lại.";
                return RedirectToAction("quanlydonhang");
            }
        }

        // Xử lý thanh toán COD
        private ActionResult ProcessCODPayment(Order order, ProductVariant variant)
        {
            try
            {
                variant.StockQuantity -= order.OrderDetails.First().Quantity;
                order.Status = OrderStatus.Confirmed;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessCODPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đơn hàng COD. Vui lòng thử lại.";
                return RedirectToAction("quanlydonhang");
            }
        }

        // GET: muahang/VNPayPending
        [HttpGet]
        public ActionResult VNPayPending(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        // GET: muahang/VNPayReturn
        [HttpGet]
        public ActionResult VNPayReturn()
        {
            try
            {
                var vnpayData = Request.QueryString;
                string vnp_SecureHash = vnpayData["vnp_SecureHash"];
                string vnp_TxnRef = vnpayData["vnp_TxnRef"];
                string vnp_ResponseCode = vnpayData["vnp_ResponseCode"];

                // Log để debug
                Debug.WriteLine($"VNPayReturn - TxnRef: {vnp_TxnRef}, ResponseCode: {vnp_ResponseCode}");

                var vnpayParams = new Dictionary<string, string>();
                foreach (string key in Request.QueryString.AllKeys)
                {
                    if (key != "vnp_SecureHash")
                    {
                        vnpayParams.Add(key, Request.QueryString[key]);
                    }
                }

                var sortedParams = vnpayParams.OrderBy(kvp => kvp.Key).ToList();
                var signData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"));
                var computedHash = HmacSHA512(VNPay_HashSecret, signData);

                if (computedHash != vnp_SecureHash)
                {
                    TempData["ErrorMessage"] = "Chữ ký không hợp lệ. Thanh toán không được xác nhận.";
                    return RedirectToAction("quanlydonhang");
                }

                if (vnp_ResponseCode == "00")
                {
                    int orderId = int.Parse(vnp_TxnRef);
                    var order = _context.Orders
                        .Include(o => o.OrderDetails)
                        .SingleOrDefault(o => o.Id == orderId);

                    if (order == null)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
                        return RedirectToAction("quanlydonhang");
                    }

                    if (order.Status != OrderStatus.Pending)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng đã được xử lý trước đó.";
                        return RedirectToAction("quanlydonhang");
                    }

                    order.Status = OrderStatus.Confirmed;
                    foreach (var detail in order.OrderDetails)
                    {
                        var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == detail.ProductVariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity -= detail.Quantity;
                        }
                    }
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Thanh toán thành công! Đơn hàng của bạn đã được xác nhận.";
                }
                else
                {
                    int orderId = int.Parse(vnp_TxnRef);
                    var order = _context.Orders
                        .Include(o => o.OrderDetails)
                        .SingleOrDefault(o => o.Id == orderId);

                    if (order != null)
                    {
                        _context.OrderDetails.RemoveRange(order.OrderDetails);
                        _context.Orders.Remove(order);
                        _context.SaveChanges();
                    }

                    TempData["ErrorMessage"] = "Thanh toán thất bại. Đơn hàng đã bị hủy.";
                }

                return RedirectToAction("quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VNPayReturn: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý kết quả thanh toán VNPay.";
                return RedirectToAction("quanlydonhang");
            }
        }

        // GET: muahang/quanlydonhang
        [HttpGet]
        public ActionResult quanlydonhang()
        {
            // Log để debug
            Debug.WriteLine($"quanlydonhang - UserId: {Session["UserId"]}");

            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Product))
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: muahang/quanlychitietdonhang
        [HttpGet]
        public ActionResult quanlychitietdonhang(int orderId)
        {
            // Log để debug
            Debug.WriteLine($"quanlychitietdonhang - UserId: {Session["UserId"]}, OrderId: {orderId}");

            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            // Truy vấn đơn hàng với các quan hệ cần thiết
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Product))
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant.ProductImages))
                .Include(o => o.ShippingAddress)
                .Include(o => o.PaymentMethod)
                .SingleOrDefault(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction("quanlydonhang");
            }

            return View(order);
        }

        // POST: muahang/CancelOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int orderId)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Truy vấn đơn hàng với các quan hệ cần thiết
                    var order = _context.Orders
                        .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                        .SingleOrDefault(o => o.Id == orderId && o.UserId == userId);

                    if (order == null)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.";
                        return RedirectToAction("quanlydonhang");
                    }

                    // Kiểm tra trạng thái đơn hàng
                    if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng đã được giao hoặc đã bị hủy, không thể hủy thêm.";
                        return RedirectToAction("quanlydonhang");
                    }

                    // Thu thập ProductId duy nhất
                    var productIds = new HashSet<int>();

                    // Hoàn lại số lượng tồn kho cho ProductVariant
                    foreach (var detail in order.OrderDetails)
                    {
                        var productVariant = detail.ProductVariant;
                        if (productVariant == null)
                        {
                            TempData["ErrorMessage"] = "Biến thể sản phẩm không tồn tại.";
                            return RedirectToAction("quanlydonhang");
                        }

                        // Hoàn lại số lượng tồn kho của biến thể
                        productVariant.StockQuantity += detail.Quantity;
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

                    // Cập nhật trạng thái đơn hàng thành Cancelled
                    order.Status = OrderStatus.Cancelled;
                    _context.Entry(order).State = EntityState.Modified;
                    _context.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
                    return RedirectToAction("quanlydonhang");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"Error in CancelOrder: {ex.Message}");
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";
                    return RedirectToAction("quanlydonhang");
                }
            }
        }

        // Helper method: Tạo chữ ký HMAC-SHA512
        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        // Helper method: Lấy địa chỉ IP của client
        private string GetClientIpAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }
            return ipAddress ?? "127.0.0.1";
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