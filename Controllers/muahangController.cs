using doanwebnangcao.Models;
using doanwebnangcao.DTO;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace doanwebnangcao.Controllers
{
    public class muahangController : Controller
    {
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
            var shippingMethods = _context.ShippingMethods
                .Where(sm => sm.IsActive)
                .ToList();

            var model = new OrderViewModel
            {
                Address = new Address()
            };

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = latestAddress;
            ViewBag.UserAddresses = userAddresses;
            ViewBag.ShippingMethods = shippingMethods;
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
            var variant = _context.ProductVariants
                .Include(v => v.Size)
                .Include(v => v.Color)
                .SingleOrDefault(v => v.Id == variantId && v.IsActive);

            if (product == null || variant == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
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
            var shippingMethods = _context.ShippingMethods
                .Where(sm => sm.IsActive)
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
            ViewBag.ShippingMethods = shippingMethods;
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
                // Log ModelState errors for debugging
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
                ViewBag.ShippingMethods = _context.ShippingMethods.Where(sm => sm.IsActive).ToList();
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
            ViewBag.SelectedAddress = address; // Set the newly added address as selected
            ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
            ViewBag.ShippingMethods = _context.ShippingMethods.Where(sm => sm.IsActive).ToList();
            return View("dathang", model);
        }

        // POST: muahang/UpdateShippingAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateShippingAddress(int ShippingAddressId, int ShippingMethodId, int productId, int variantId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];
            var address = _context.Addresses
                .SingleOrDefault(a => a.Id == ShippingAddressId && a.UserId == userId);
            var shippingMethod = _context.ShippingMethods
                .SingleOrDefault(sm => sm.Id == ShippingMethodId && sm.IsActive);
            var product = _context.Products
                .SingleOrDefault(p => p.Id == productId && p.IsActive);
            var variant = _context.ProductVariants
                .SingleOrDefault(v => v.Id == variantId && v.IsActive);

            if (address == null || shippingMethod == null || product == null || variant == null)
            {
                TempData["ErrorMessage"] = "Địa chỉ hoặc phương thức vận chuyển không hợp lệ.";
                return RedirectToAction("dathang");
            }

            var model = new OrderViewModel
            {
                ProductId = productId,
                VariantId = variantId,
                Quantity = quantity,
                Product = product,
                Variant = variant,
                Address = address
            };

            ViewBag.PaymentMethods = _context.PaymentMethods.Where(pm => pm.IsActive).ToList();
            ViewBag.SelectedAddress = address;
            ViewBag.UserAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
            ViewBag.ShippingMethods = _context.ShippingMethods.Where(sm => sm.IsActive).ToList();
            TempData["SelectedShippingMethodId"] = ShippingMethodId;
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

            if (AcceptTerms != true) // Kiểm tra nếu không đồng ý hoặc giá trị là null
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
                ViewBag.ShippingMethods = _context.ShippingMethods.Where(sm => sm.IsActive).ToList();
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

            var unitPrice = variant.VariantPrice ?? product.Price;
            var subtotal = unitPrice * model.Quantity;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = subtotal + 4, // Shipping cost mặc định là 4
                Status = OrderStatus.Pending, // Trạng thái mặc định là Pending
                ShippingAddressId = address.Id,
                ShippingMethodId = 1220, // Đặt ShippingMethodId là null
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
            variant.StockQuantity -= model.Quantity;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("quanlydonhang");
        }

        // GET: muahang/quanlydonhang
        [HttpGet]
        public ActionResult quanlydonhang()
        {
            // Kiểm tra session
            if (Session["UserId"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            // Truy vấn danh sách đơn hàng của người dùng
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant))
                .Include(o => o.OrderDetails.Select(od => od.ProductVariant.Product))
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
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