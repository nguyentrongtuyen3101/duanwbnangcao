using System;
using System.Linq;
using System.Web.Mvc;
using doanwebnangcao.Models;

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

            // Lấy thông tin người dùng từ cơ sở dữ liệu
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
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