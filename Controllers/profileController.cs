using System;
using System.Linq;
using System.Web.Mvc;
using doanwebnangcao.Models;
using System.Data.Entity; // Thêm namespace này để sử dụng Include

namespace doanwebnangcao.Controllers
{
    public class profileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public profileController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: profile/trangcanhan
        public ActionResult trangcanhan()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            // Lấy thông tin người dùng từ cơ sở dữ liệu, bao gồm các quan hệ
            var user = _context.Users
                .Include(u => u.Orders.Select(o => o.OrderDetails.Select(od => od.ProductVariant.Product))) // Tải Orders, OrderDetails, ProductVariant, Product
                .Include(u => u.Orders.Select(o => o.OrderDetails.Select(od => od.ProductVariant.ProductImages))) // Tải ProductImages của ProductVariant
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            return View(user);
        }

        // GET: profile/editprofile
        public ActionResult editprofile()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            // Lấy thông tin người dùng từ cơ sở dữ liệu
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            return View(user);
        }

        // POST: profile/editprofile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult editprofile(User model)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Lấy UserId từ session
                int userId = (int)Session["UserId"];

                // Tìm người dùng trong cơ sở dữ liệu
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                // Cập nhật thông tin người dùng
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Birthday = model.Birthday;
                user.Gender = model.Gender;
                user.PhoneNumber = model.PhoneNumber;

                // Lưu thay đổi vào cơ sở dữ liệu
                _context.SaveChanges();

                // Cập nhật lại session
                Session["FullName"] = $"{user.FirstName} {user.LastName}";

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("trangcanhan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật hồ sơ: " + ex.Message);
                return View(model);
            }
        }

        // GET: profile/myprofile
        public ActionResult myprofile()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            // Lấy thông tin người dùng từ cơ sở dữ liệu, bao gồm các quan hệ
            var user = _context.Users
                .Include(u => u.Orders) // Tải Orders để tính Orders Placed và Cancel Orders
                .Include(u => u.Wishlists) // Tải Wishlists để tính số lượng Wishlist
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Truyền dữ liệu OrdersCount và WishlistCount vào ViewBag để hiển thị trong sidebar
            ViewBag.OrdersCount = user.Orders?.Count ?? 0;
            ViewBag.WishlistCount = user.Wishlists?.Count ?? 0;

            return View(user);
        }

        // GET: profile/editaddress
        public ActionResult editaddress()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            // Lấy danh sách địa chỉ của người dùng
            var addresses = _context.Addresses
                .Where(a => a.UserId == userId)
                .ToList();

            // Lấy thông tin người dùng để điền vào ViewBag
            var user = _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Wishlists)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Truyền dữ liệu OrdersCount và WishlistCount vào ViewBag để hiển thị trong sidebar
            ViewBag.OrdersCount = user.Orders?.Count ?? 0;
            ViewBag.WishlistCount = user.Wishlists?.Count ?? 0;

            return View(addresses);
        }

        // GET: profile/adddiachi
        public ActionResult adddiachi(int? id)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            // Lấy thông tin người dùng để điền vào ViewBag
            var user = _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Wishlists)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Truyền dữ liệu OrdersCount và WishlistCount vào ViewBag để hiển thị trong sidebar
            ViewBag.OrdersCount = user.Orders?.Count ?? 0;
            ViewBag.WishlistCount = user.Wishlists?.Count ?? 0;

            // Nếu có id, đây là chỉnh sửa địa chỉ
            if (id.HasValue)
            {
                var address = _context.Addresses.FirstOrDefault(a => a.Id == id && a.UserId == userId);
                if (address == null)
                {
                    return HttpNotFound("Address not found.");
                }
                ViewBag.IsEdit = true; // Đánh dấu là chỉnh sửa
                return View(address);
            }

            // Nếu không có id, đây là thêm mới
            ViewBag.IsEdit = false;
            return View(new Address());
        }

        // POST: profile/adddiachi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult adddiachi(Address model)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            // Lấy UserId từ session
            int userId = (int)Session["UserId"];

            if (!ModelState.IsValid)
            {
                // Lấy thông tin người dùng để điền vào ViewBag nếu có lỗi
                var user = _context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.Wishlists)
                    .FirstOrDefault(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                ViewBag.OrdersCount = user.Orders?.Count ?? 0;
                ViewBag.WishlistCount = user.Wishlists?.Count ?? 0;
                ViewBag.IsEdit = model.Id > 0; // Giữ trạng thái chỉnh sửa nếu có lỗi

                return View(model);
            }

            try
            {
                if (model.Id > 0) // Trường hợp cập nhật
                {
                    var existingAddress = _context.Addresses.FirstOrDefault(a => a.Id == model.Id && a.UserId == userId);
                    if (existingAddress == null)
                    {
                        return HttpNotFound("Address not found.");
                    }

                    // Cập nhật thông tin địa chỉ
                    existingAddress.FullName = model.FullName;
                    existingAddress.PhoneNumber = model.PhoneNumber;
                    existingAddress.AddressLine = model.AddressLine;
                    existingAddress.City = model.City;
                    existingAddress.Country = model.Country;

                    TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
                }
                else // Trường hợp thêm mới
                {
                    // Gán UserId cho địa chỉ
                    model.UserId = userId;

                    // Nếu đây là địa chỉ đầu tiên của người dùng, đặt nó làm mặc định
                    var existingAddresses = _context.Addresses.Where(a => a.UserId == userId).ToList();
                    if (!existingAddresses.Any())
                    {
                        model.IsDefault = true;
                    }

                    // Thêm địa chỉ mới vào cơ sở dữ liệu
                    _context.Addresses.Add(model);

                    TempData["SuccessMessage"] = "Thêm địa chỉ thành công!";
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                _context.SaveChanges();

                return RedirectToAction("editaddress");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);

                // Lấy thông tin người dùng để điền vào ViewBag nếu có lỗi
                var user = _context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.Wishlists)
                    .FirstOrDefault(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("DangNhap", "Home");
                }

                ViewBag.OrdersCount = user.Orders?.Count ?? 0;
                ViewBag.WishlistCount = user.Wishlists?.Count ?? 0;
                ViewBag.IsEdit = model.Id > 0; // Giữ trạng thái chỉnh sửa nếu có lỗi

                return View(model);
            }
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