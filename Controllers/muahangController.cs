using doanwebnangcao.Models;
using doanwebnangcao.DTO;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using Newtonsoft.Json;
using System.Configuration;
using webbanxe.Payments;
using System.Text;
using System.Security.Claims;

namespace doanwebnangcao.Controllers
{
    public class MuahangController : Controller
    {
        private readonly string VNPay_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
        private readonly string VNPay_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
        private readonly string VNPay_Url = ConfigurationManager.AppSettings["vnp_Url"];
        private readonly string VNPay_Api = ConfigurationManager.AppSettings["vnp_Api"];
        private readonly string VNPay_ReturnUrl = ConfigurationManager.AppSettings["vnp_ReturnUrl"];

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
                var productVariant = _context.ProductVariants
                    .Include(pv => pv.Product)
                    .Include(pv => pv.Size)
                    .Include(pv => pv.Color)
                    .SingleOrDefault(pv => pv.Id == variantId && pv.ProductId == productId);

                if (productVariant == null)
                    return ErrorRedirect("Sản phẩm hoặc biến thể không tồn tại.", productId);

                if (productVariant.StockQuantity < quantity)
                    return ErrorRedirect($"Sản phẩm {productVariant.Product.Name} không đủ số lượng tồn kho.", productId);

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

                TempData["BuyNowItem"] = JsonConvert.SerializeObject(buyNowItem);
                return RedirectToAction("Dathang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in BuyNow: {ex.Message}");
                return ErrorRedirect("Có lỗi xảy ra khi xử lý yêu cầu mua ngay. Vui lòng thử lại.", productId);
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

            var model = new OrderViewModel { Address = new Address() };
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

            if (!string.IsNullOrEmpty(model.Address.PhoneNumber) && !Regex.IsMatch(model.Address.PhoneNumber, @"^\d{10,15}$"))
            {
                ModelState.AddModelError("Address.PhoneNumber", "Số điện thoại phải từ 10-15 chữ số và chỉ chứa số.");
            }

            Cart cart = null;
            BuyNowItem buyNowItem = null;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Debug.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                return LoadDathangView(model, userId, "Thông tin không hợp lệ. Vui lòng kiểm tra lại.", ref cart, ref buyNowItem);
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
            return LoadDathangView(model, userId, null, ref cart, ref buyNowItem, address);
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
                var address = _context.Addresses
                    .SingleOrDefault(a => a.Id == ShippingAddressId && a.UserId == userId);

                if (address == null)
                {
                    TempData["ErrorMessage"] = "Địa chỉ không tồn tại hoặc không thuộc về bạn.";
                    return RedirectToAction("Dathang");
                }

                var userAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
                var model = new OrderViewModel { Address = new Address() };
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
            Cart cart = null;
            BuyNowItem buyNowItem = null;

            if (AcceptTerms != true)
            {
                TempData["ErrorMessage"] = "Vui lòng đồng ý với điều khoản dịch vụ.";
                return LoadDathangView(model, userId, null, ref cart, ref buyNowItem);
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

            if (TempData["BuyNowItem"] != null)
            {
                var buyNowItemJson = TempData["BuyNowItem"].ToString();
                buyNowItem = JsonConvert.DeserializeObject<BuyNowItem>(buyNowItemJson);
            }

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

            if (buyNowItem != null)
            {
                order.TotalAmount = buyNowItem.UnitPrice * buyNowItem.Quantity + 4;
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
                order.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity) + 4;
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
                TempData["ErrorMessage"] = "Đặt hàng thành công,đơn hàng đang chờ xử lý!";
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // Xử lý thanh toán VNPay
        private ActionResult ProcessVNPayPayment(Order order)
        {
            try
            {
                if (order.TotalAmount <= 0)
                    throw new Exception("Số tiền thanh toán không hợp lệ.");

                string vnp_TxnRef = $"{order.Id}_{DateTime.Now.Ticks}";
                string vnp_OrderInfo = $"Thanh toan don hang {order.Id}";
                long vnp_Amount = (long)(order.TotalAmount * 100);
                string vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");

                var vnpay = new VnPay();
                vnpay.AddRequestData("vnp_Version", VnPay.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", VNPay_TmnCode);
                vnpay.AddRequestData("vnp_Amount", vnp_Amount.ToString());
                vnpay.AddRequestData("vnp_CreateDate", vnp_CreateDate);
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HashAndGetIP.GetIpAddress(HttpContext));
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
                vnpay.AddRequestData("vnp_OrderType", "250000");
                vnpay.AddRequestData("vnp_ReturnUrl", VNPay_ReturnUrl);
                vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);

                // Log các tham số trước khi tạo chữ ký
                StringBuilder paramsLog = new StringBuilder("VNPay Params Before Hash: ");
                foreach (var param in vnpay.GetType().GetField("requestData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(vnpay) as SortedList<string, string>)
                {
                    paramsLog.Append($"{param.Key}={param.Value}&");
                }
                Debug.WriteLine(paramsLog.ToString().TrimEnd('&'));

                // Log thêm thông tin hệ thống
                Debug.WriteLine($"System Time: {DateTime.Now.ToString("yyyyMMddHHmmss")}");
                Debug.WriteLine($"System TimeZone: {TimeZoneInfo.Local.Id}");
                Debug.WriteLine($"VNPay Params - TxnRef: {vnp_TxnRef}, Amount: {vnp_Amount}, CreateDate: {vnp_CreateDate}");
                Debug.WriteLine($"ReturnUrl: {VNPay_ReturnUrl}");

                string paymentUrl = vnpay.CreateRequestUrl(VNPay_Url, VNPay_HashSecret);
                Debug.WriteLine($"Payment URL: {paymentUrl}");

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessVNPayPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi khi khởi tạo thanh toán VNPay. Vui lòng thử lại.";
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
                        variant.StockQuantity -= buyNowItem.Quantity;
                }
                else if (cart != null)
                {
                    foreach (var cartDetail in cart.CartDetails)
                    {
                        var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == cartDetail.ProductVariantId);
                        if (variant != null)
                            variant.StockQuantity -= cartDetail.Quantity;
                    }
                    _context.CartDetails.RemoveRange(cart.CartDetails);
                    _context.Carts.Remove(cart);
                }

                order.Status = OrderStatus.Confirmed;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Đặt hàng thành công và chờ xử lý!";
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessCODPayment: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đơn hàng COD. Vui lòng thử lại.";
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // GET: Muahang/VNPayReturn
        [Authorize]
        [HttpGet]
        public ActionResult VNPayReturn()
        {
            try
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                var userId = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                    return RedirectToAction("DangNhap", "Home");
                }

                Session["UserId"] = parsedUserId;

                var vnpay = new VnPay();
                foreach (string key in Request.QueryString.AllKeys)
                {
                    if (!string.IsNullOrEmpty(Request.QueryString[key]))
                    {
                        vnpay.AddResponseData(key, Request.QueryString[key]);
                    }
                }
                string vnp_SecureHash = vnpay.GetResponseData("vnp_SecureHash");
                string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_Amount = vnpay.GetResponseData("vnp_Amount");

                Debug.WriteLine($"VNPayReturn - TxnRef: {vnp_TxnRef}, ResponseCode: {vnp_ResponseCode}, Amount: {vnp_Amount}");

                if (string.IsNullOrEmpty(vnp_SecureHash) || string.IsNullOrEmpty(vnp_TxnRef) || string.IsNullOrEmpty(vnp_ResponseCode))
                {
                    TempData["ErrorMessage"] = "Dữ liệu trả về từ VNPay không hợp lệ.";
                    return RedirectToAction("Quanlydonhang");
                }

                if (!vnpay.ValidateSignature(vnp_SecureHash, VNPay_HashSecret))
                {
                    Debug.WriteLine($"Invalid signature: {vnp_SecureHash}");
                    TempData["ErrorMessage"] = "Chữ ký không hợp lệ. Thanh toán không được xác nhận.";
                    return RedirectToAction("Quanlydonhang");
                }

                string[] txnRefParts = vnp_TxnRef.Split('_');
                if (txnRefParts.Length == 0 || !int.TryParse(txnRefParts[0], out int orderId))
                {
                    TempData["ErrorMessage"] = "Mã giao dịch không hợp lệ.";
                    return RedirectToAction("Quanlydonhang");
                }

                var order = _context.Orders
                    .Include(o => o.OrderDetails)
                    .SingleOrDefault(o => o.Id == orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
                    return RedirectToAction("Quanlydonhang");
                }

                if (order.Status != OrderStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Đơn hàng đã được xử lý trước đó.";
                    return RedirectToAction("Quanlydonhang");
                }

                long vnpAmount = long.Parse(vnp_Amount) / 100;
                if (vnpAmount != (long)order.TotalAmount)
                {
                    TempData["ErrorMessage"] = "Số tiền thanh toán không khớp với đơn hàng.";
                    return RedirectToAction("Quanlydonhang");
                }

                if (vnp_ResponseCode == "00")
                {
                    order.Status = OrderStatus.Confirmed;
                    foreach (var detail in order.OrderDetails)
                    {
                        var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == detail.ProductVariantId);
                        if (variant != null)
                            variant.StockQuantity -= detail.Quantity;
                    }

                    var cart = _context.Carts
                        .Include(c => c.CartDetails)
                        .SingleOrDefault(c => c.UserId == order.UserId);
                    if (cart != null)
                    {
                        _context.CartDetails.RemoveRange(cart.CartDetails);
                        _context.Carts.Remove(cart);
                    }

                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Thanh toán và đặt hàng thành công và chờ xử lý!";
                }
                else
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails);
                    _context.Orders.Remove(order);
                    _context.SaveChanges();
                    TempData["ErrorMessage"] = $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}.";
                }

                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VNPayReturn: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý kết quả thanh toán VNPay.";
                TempData.Remove("BuyNowItem");
                return RedirectToAction("Quanlydonhang");
            }
        }

        // POST: Muahang/QueryTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QueryTransaction(string txnRef, string orderId)
        {
            try
            {
                if (!int.TryParse(orderId, out int parsedOrderId))
                {
                    TempData["ErrorMessage"] = "Mã đơn hàng không hợp lệ.";
                    return RedirectToAction("Quanlydonhang");
                }

                var order = _context.Orders.SingleOrDefault(o => o.Id == parsedOrderId && o.UserId == (int)Session["UserId"]);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.";
                    return RedirectToAction("Quanlydonhang");
                }

                var vnpay = new VnPay();
                vnpay.AddRequestData("vnp_Version", VnPay.VERSION);
                vnpay.AddRequestData("vnp_Command", "querydr");
                vnpay.AddRequestData("vnp_TmnCode", VNPay_TmnCode);
                vnpay.AddRequestData("vnp_TxnRef", txnRef);
                vnpay.AddRequestData("vnp_OrderInfo", $"Tra cứu giao dịch cho đơn hàng {orderId}");
                vnpay.AddRequestData("vnp_TransDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_IpAddr", HashAndGetIP.GetIpAddress(HttpContext));

                using (var client = new System.Net.WebClient())
                {
                    var queryString = vnpay.CreateRequestUrl("", VNPay_HashSecret).TrimStart('?');
                    client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    var response = client.UploadString(VNPay_Api, "POST", queryString);
                    Debug.WriteLine($"Phản hồi tra cứu VNPay: {response}");

                    var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                    if (responseDict.ContainsKey("vnp_ResponseCode") && responseDict["vnp_ResponseCode"] == "00")
                    {
                        if (order.Status == OrderStatus.Pending)
                        {
                            order.Status = OrderStatus.Confirmed;
                            foreach (var detail in order.OrderDetails)
                            {
                                var variant = _context.ProductVariants.SingleOrDefault(v => v.Id == detail.ProductVariantId);
                                if (variant != null)
                                    variant.StockQuantity -= detail.Quantity;
                            }
                            _context.SaveChanges();
                            TempData["SuccessMessage"] = "Tra cứu giao dịch thành công! Đơn hàng đã được xác nhận.";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Giao dịch đã được xác nhận trước đó.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Tra cứu giao dịch thất bại. Mã lỗi: {responseDict["vnp_ResponseCode"]}.";
                    }
                }

                return RedirectToAction("Quanlydonhang");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi trong QueryTransaction: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tra cứu giao dịch VNPay.";
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

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không thể hủy do đã ở trạng thái vận chuyển, đã giao hoặc đã bị hủy.";
                    return RedirectToAction("Quanlydonhang");
                }

                order.Status = OrderStatus.Cancelled;
                _context.Entry(order).State = EntityState.Modified;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "HUY DON HANG THANH CONG!";
                return RedirectToAction("Quanlychitietdonhang", new { orderId = orderId });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CancelOrder: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Quanlydonhang");
            }
        }

        // Helper method: Redirect with error message
        private ActionResult ErrorRedirect(string message, int productId)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("sanphamdetailt", "HangMoiVe", new { id = productId });
        }

        // Helper method: Load Dathang view with data
        private ActionResult LoadDathangView(OrderViewModel model, int userId, string errorMessage, ref Cart cart, ref BuyNowItem buyNowItem, Address selectedAddress = null)
        {
            if (!string.IsNullOrEmpty(errorMessage))
                TempData["ErrorMessage"] = errorMessage;

            var latestAddress = selectedAddress ?? _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();
            var userAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();

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
            ViewBag.SelectedAddress = latestAddress;
            ViewBag.UserAddresses = userAddresses;
            return View("Dathang", model);
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