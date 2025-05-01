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
using Newtonsoft.Json;

namespace doanwebnangcao.Controllers
{
    public class MuahangController : Controller
    {
        private const string VNPay_TmnCode = "3D8U719S";
        private const string VNPay_HashSecret = "14X61NZKV0USRUXHQPYYM2MSFZDHGNLJ";
        private const string VNPay_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private const string VNPay_ReturnUrl = "http://localhost/Muahang/VNPayPending"; // Cần thay bằng URL công khai khi thử nghiệm VNPay

        private readonly ApplicationDbContext _context;
        public MuahangController()
        {
            _context = new ApplicationDbContext();
        }

        // POST: Muahang/BuyNow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuyNow(int productId, int variantId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            try
            {
                // Kiểm tra biến thể sản phẩm
                var productVariant = _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .SingleOrDefault(pv => pv.Id == variantId && pv.ProductId == productId);

                if (productVariant == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm hoặc biến thể không tồn tại.";
                    return RedirectToAction("sanphamdetailt", "HangMoiVe", new { id = productId });
                }

                // Kiểm tra số lượng tồn kho
                if (productVariant.StockQuantity < quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {productVariant.Product.Name} không đủ số lượng tồn kho.";
                    return RedirectToAction("sanphamdetailt", "HangMoiVe", new { id = productId });
                }

                // Tạo đối tượng tạm thời để lưu thông tin sản phẩm
                var buyNowItem = new BuyNowItem
                {
                    ProductVariantId = variantId,
                    Quantity = quantity,
                    UnitPrice = productVariant.VariantDiscountedPrice ?? productVariant.VariantPrice ?? productVariant.Product.DiscountedPrice ?? productVariant.Product.Price,
                    ProductName = productVariant.Product.Name,
                    VariantImageUrl = productVariant.VariantImageUrl ?? productVariant.Product.ImageUrl ?? "/images/placeholder.jpg",
                    SizeName = productVariant.Size?.Name,
                    ColorName = productVariant.Color?.Name
                };

                // Lưu vào TempData để sử dụng trong action Dathang
                TempData["BuyNowItem"] = JsonConvert.SerializeObject(buyNowItem);

                // Chuyển hướng đến trang đặt hàng
                return RedirectToAction("Dathang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in BuyNow: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý yêu cầu mua ngay. Vui lòng thử lại.";
                return RedirectToAction("sanphamdetailt", "HangMoiVe", new { id = productId });
            }
        }

        // GET: Muahang/Dathang
        [HttpGet]
        public ActionResult Dathang()
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

            // Khai báo biến cart một lần
            Cart cart = null;
            BuyNowItem buyNowItem = null;

            // Kiểm tra xem có BuyNowItem trong TempData không
            if (TempData["BuyNowItem"] != null)
            {
                var buyNowItemJson = TempData["BuyNowItem"].ToString();
                buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
                ViewBag.BuyNowItem = buyNowItem;
                // Giữ TempData để sử dụng trong PlaceOrder
                TempData["BuyNowItem"] = buyNowItemJson;
            }
            else
            {
                // Nếu không có BuyNowItem, sử dụng giỏ hàng
                cart = _context.Carts
                    .Include(c => c.CartDetails)
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                    .SingleOrDefault(c => c.UserId == userId);
                ViewBag.Cart = cart;
            }

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = latestAddress;
            ViewBag.UserAddresses = userAddresses;
            return View(model);
        }

        // POST: Muahang/SaveAddressAndOrder
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

            // Khai báo biến cart và buyNowItem một lần
            Cart cart = null;
            BuyNowItem buyNowItem = null;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Debug.WriteLine("ModelState Errors: " + string.Join(", ", errors));

                TempData["ErrorMessage"] = "Thông tin không hợp lệ. Vui lòng kiểm tra lại.";
                ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
                ViewBag.SelectedAddress = _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault();
                ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();

                if (TempData["BuyNowItem"] != null)
                {
                    var buyNowItemJson = TempData["BuyNowItem"].ToString();
                    buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
                    ViewBag.BuyNowItem = buyNowItem;
                    TempData["BuyNowItem"] = buyNowItemJson;
                }
                else
                {
                    cart = _context.Carts
                        .Include(c => c.CartDetails)
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                        .SingleOrDefault(c => c.UserId == userId);
                    ViewBag.Cart = cart;
                }

                return View("Dathang", model);
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
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = address;
            ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();

            if (TempData["BuyNowItem"] != null)
            {
                var buyNowItemJson = TempData["BuyNowItem"].ToString();
                buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
                ViewBag.BuyNowItem = buyNowItem;
                TempData["BuyNowItem"] = buyNowItemJson;
            }
            else
            {
                cart = _context.Carts
                    .Include(c => c.CartDetails)
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                    .SingleOrDefault(c => c.UserId == userId);
                ViewBag.Cart = cart;
            }

            return View("Dathang", model);
        }

        // POST: Muahang/UpdateShippingAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateShippingAddress(int ShippingAddressId)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            try
            {
                // Kiểm tra địa chỉ có thuộc về người dùng không
                var address = _context.Addresses
                    .SingleOrDefault(a => a.Id == ShippingAddressId && a.UserId == userId);

                if (address == null)
                {
                    TempData["ErrorMessage"] = "Địa chỉ không tồn tại hoặc không thuộc về bạn.";
                    return RedirectToAction("Dathang");
                }

                // Load lại trang đặt hàng với địa chỉ đã chọn
                var userAddresses = _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToList();
                var model = new OrderViewModel
                {
                    Address = new Address()
                };

                // Khai báo biến cart và buyNowItem một lần
                Cart cart = null;
                BuyNowItem buyNowItem = null;

                if (TempData["BuyNowItem"] != null)
                {
                    var buyNowItemJson = TempData["BuyNowItem"].ToString();
                    buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
                    ViewBag.BuyNowItem = buyNowItem;
                    TempData["BuyNowItem"] = buyNowItemJson;
                }
                else
                {
                    cart = _context.Carts
                        .Include(c => c.CartDetails)
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                        .SingleOrDefault(c => c.UserId == userId);
                    ViewBag.Cart = cart;
                }

                ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
                ViewBag.SelectedAddress = address;
                ViewBag.UserAddresses = userAddresses;

                TempData["SuccessMessage"] = "Đã cập nhật địa chỉ giao hàng.";
                return View("Dathang", model);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateShippingAddress: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật địa chỉ. Vui lòng thử lại.";
                return RedirectToAction("Dathang");
            }
        }

        // POST: Muahang/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(OrderViewModel model, int PaymentMethodId, bool? AcceptTerms)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            // Khai báo biến cart và buyNowItem một lần
            Cart cart = null;
            BuyNowItem buyNowItem = null;

            if (AcceptTerms != true)
            {
                TempData["ErrorMessage"] = "Vui lòng đồng ý với điều khoản dịch vụ.";
                var latestAddress = _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault();
                var userAddresses = _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToList();
                ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
                ViewBag.SelectedAddress = latestAddress;
                ViewBag.UserAddresses = userAddresses;

                if (TempData["BuyNowItem"] != null)
                {
                    var buyNowItemJson = TempData["BuyNowItem"].ToString();
                    buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
                    ViewBag.BuyNowItem = buyNowItem;
                    TempData["BuyNowItem"] = buyNowItemJson;
                }
                else
                {
                    cart = _context.Carts
                        .Include(c => c.CartDetails)
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                        .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                        .SingleOrDefault(c => c.UserId == userId);
                    ViewBag.Cart = cart;
                }

                return View("Dathang", model);
            }

            var paymentMethod = _context.PaymentMethods.SingleOrDefault(pm => pm.Id == PaymentMethodId);
            if (paymentMethod == null)
            {
                TempData["ErrorMessage"] = "Phương thức thanh toán không tồn tại.";
                return RedirectToAction("Dathang");
            }

            var address = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (address == null)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập địa chỉ giao hàng.";
                return RedirectToAction("Dathang");
            }

            // Kiểm tra xem có BuyNowItem không
            if (TempData["BuyNowItem"] != null)
            {
                var buyNowItemJson = TempData["BuyNowItem"].ToString();
                buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
            }

            // Nếu không có BuyNowItem, lấy giỏ hàng
            if (buyNowItem == null)
            {
                cart = _context.Carts
                    .Include(c => c.CartDetails)
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                    .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                    .SingleOrDefault(c => c.UserId == userId);

                if (cart == null || !cart.CartDetails.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống.";
                    return RedirectToAction("Dathang");
                }
            }

            // Kiểm tra số lượng tồn kho
            if (buyNowItem != null)
            {
                var productVariant = _context.ProductVariants
                    .SingleOrDefault(v => v.Id == buyNowItem.ProductVariantId);

                if (productVariant == null || productVariant.StockQuantity < buyNowItem.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm {buyNowItem.ProductName} không đủ số lượng tồn kho.";
                    return RedirectToAction("Dathang");
                }
            }
            else
            {
                foreach (var cartDetail in cart.CartDetails)
                {
                    if (cartDetail.ProductVariant.StockQuantity < cartDetail.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm {cartDetail.ProductVariant.Product.Name} không đủ số lượng.";
                        return RedirectToAction("Dathang");
                    }
                }
            }

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                ShippingAddressId = address.Id,
                ShippingCost = 4,
                PaymentMethodId = PaymentMethodId,
                Notes = model.OrderNote
            };

            // Tạo chi tiết đơn hàng
            if (buyNowItem != null)
            {
                order.TotalAmount = buyNowItem.UnitPrice * buyNowItem.Quantity + 4; // Phí vận chuyển
                var orderDetail = new OrderDetail
                {
                    Order = order,
                    ProductVariantId = buyNowItem.ProductVariantId,
                    Quantity = buyNowItem.Quantity,
                    UnitPrice = buyNowItem.UnitPrice,
                    Subtotal = buyNowItem.UnitPrice * buyNowItem.Quantity
                };
                _context.OrderDetails.Add(orderDetail);
            }
            else
            {
                order.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity) + 4; // Phí vận chuyển
                foreach (var cartDetail in cart.CartDetails)
                {
                    var orderDetail = new OrderDetail
                    {
                        Order = order,
                        ProductVariantId = cartDetail.ProductVariantId,
                        Quantity = cartDetail.Quantity,
                        UnitPrice = cartDetail.UnitPrice,
                        Subtotal = cartDetail.UnitPrice * cartDetail.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Xử lý thanh toán
            if (string.Equals(paymentMethod.Name, "Thanh toán qua VNP", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessVNPayPayment(order);
            }
            else if (string.Equals(paymentMethod.Name, "Thanh toán khi nhận hàng", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessCODPayment(order, cart, buyNowItem);
            }
            else
            {
                TempData["ErrorMessage"] = "Phương thức thanh toán không được hỗ trợ, nhưng đơn hàng đã được ghi nhận.";
                // Xóa TempData["BuyNowItem"] sau khi hoàn tất đơn hàng
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
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
                    { "vnp_SecureHashType", "SHA512" }
                };

                var sortedParams = vnpayParams.OrderBy(kvp => kvp.Key).ToList();
                var signData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"));
                var vnp_SecureHash = HmacSHA512(VNPay_HashSecret, signData);
                vnpayParams.Remove("vnp_SecureHashType");
                vnpayParams.Add("vnp_SecureHash", vnp_SecureHash);

                var paymentUrl = VNPay_Url + "?" + string.Join("&", vnpayParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"));
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessVNPayPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán VNPay. Vui lòng thử lại.";
                // Xóa TempData["BuyNowItem"] nếu có lỗi
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // Xử lý thanh toán COD
        private ActionResult ProcessCODPayment(Order order, Cart cart, BuyNowItem buyNowItem)
        {
            try
            {
                if (buyNowItem != null)
                {
                    var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == buyNowItem.ProductVariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity -= buyNowItem.Quantity;
                    }
                }
                else if (cart != null)
                {
                    foreach (var cartDetail in cart.CartDetails)
                    {
                        var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == cartDetail.ProductVariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity -= cartDetail.Quantity;
                        }
                    }
                    // Xóa giỏ hàng
                    _context.CartDetails.RemoveRange(cart.CartDetails);
                    _context.Carts.Remove(cart);
                }

                order.Status = OrderStatus.Confirmed;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                // Xóa TempData["BuyNowItem"] sau khi hoàn tất đơn hàng
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessCODPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đơn hàng COD. Vui lòng thử lại.";
                // Xóa TempData["BuyNowItem"] nếu có lỗi
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // GET: Muahang/VNPayPending
        [HttpGet]
        public ActionResult VNPayPending(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        // GET: Muahang/VNPayReturn
        [HttpGet]
        public ActionResult VNPayReturn()
        {
            try
            {
                var vnpayData = Request.QueryString;
                string vnp_SecureHash = vnpayData["vnp_SecureHash"];
                string vnp_TxnRef = vnpayData["vnp_TxnRef"];
                string vnp_ResponseCode = vnpayData["vnp_ResponseCode"];

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
                    // Xóa TempData["BuyNowItem"] nếu có lỗi
                    TempData.Remove("BuyNowItem");
                    return RedirectToAction("Quanlydonhang");
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
                        // Xóa TempData["BuyNowItem"] nếu có lỗi
                        TempData.Remove("BuyNowItem");
                        return RedirectToAction("Quanlydonhang");
                    }

                    if (order.Status != OrderStatus.Pending)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng đã được xử lý trước đó.";
                        // Xóa TempData["BuyNowItem"] nếu có lỗi
                        TempData.Remove("BuyNowItem");
                        return RedirectToAction("Quanlydonhang");
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

                    // Xóa giỏ hàng nếu không phải BuyNow
                    var cart = _context.Carts
                        .Include(c => c.CartDetails)
                        .SingleOrDefault(c => c.UserId == order.UserId);
                    if (cart != null)
                    {
                        _context.CartDetails.RemoveRange(cart.CartDetails);
                        _context.Carts.Remove(cart);
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

                // Xóa TempData["BuyNowItem"] sau khi hoàn tất thanh toán VNPay
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VNPayReturn: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý kết quả thanh toán VNPay.";
                // Xóa TempData["BuyNowItem"] nếu có lỗi
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // GET: Muahang/Quanlydonhang
        [HttpGet]
        public ActionResult Quanlydonhang()
        {
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

        // GET: Muahang/Quanlychitietdonhang
        [HttpGet]
        public ActionResult Quanlychitietdonhang(int orderId)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

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
                return RedirectToAction("Quanlydonhang");
            }

            return View(order);
        }

        // POST: Muahang/CancelOrder
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

            try
            {
                var order = _context.Orders
                    .SingleOrDefault(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.";
                    return RedirectToAction("Quanlydonhang");
                }

                // Chỉ cho phép hủy nếu trạng thái là Pending hoặc Confirmed
                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không thể hủy do đã ở trạng thái vận chuyển, đã giao hoặc đã bị hủy.";
                    return RedirectToAction("Quanlydonhang");
                }

                // Cập nhật trạng thái đơn hàng thành Cancelled
                order.Status = OrderStatus.Cancelled;
                _context.Entry(order).State = EntityState.Modified;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CancelOrder: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Quanlydonhang");
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