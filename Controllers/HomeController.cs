using BCrypt.Net;
using doanwebnangcao.Models;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using static System.Net.WebRequestMethods;

namespace doanwebnangcao.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController()
        {
            System.Diagnostics.Debug.WriteLine("HomeController constructor called.");
            try
            {
                _context = new ApplicationDbContext();
                System.Diagnostics.Debug.WriteLine("ApplicationDbContext initialized successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error initializing ApplicationDbContext: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                System.Diagnostics.Debug.WriteLine("Inner Exception: " + (ex.InnerException?.Message ?? "No inner exception"));
                throw;
            }
        }

        public ActionResult DangNhap()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public ActionResult DangNhap(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email không tồn tại.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu không đúng.");
                    return View(model);
                }

                SignInUser(user);
                System.Diagnostics.Debug.WriteLine("Session UserId: " + HttpContext.Session["UserId"]);
                return RedirectToRoleBasedPage(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in DangNhap: " + ex.Message);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại.";
                return View(model);
            }
        }

        public ActionResult DangKy()
        {
            return View();
        }

        public ActionResult TestDatabase()
        {
            try
            {
                var userCount = _context.Users.Count();
                return Content("Kết nối cơ sở dữ liệu thành công. Số lượng người dùng: " + userCount);
            }
            catch (Exception ex)
            {
                return Content("Kết nối cơ sở dữ liệu thất bại: " + ex.Message);
            }
        }

        [HttpPost]
        public ActionResult DangKy(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng.");
                    return View(model);
                }

                if (model.Birthday == default)
                {
                    ModelState.AddModelError("Birthday", "Ngày sinh không hợp lệ.");
                    return View(model);
                }

                if (model.Birthday > DateTime.Now)
                {
                    ModelState.AddModelError("Birthday", "Ngày sinh không được nằm trong tương lai.");
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                var userCount = _context.Users.Count();
                var role = userCount == 0 ? Role.Admin : Role.User;

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Birthday = model.Birthday,
                    Gender = model.Gender,
                    Email = model.Email,
                    Password = hashedPassword,
                    Role = role,
                    ExternalId = null,
                    Provider = null
                };

                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("DangNhap", "Home");
            }
            return View(model);
        }

        public ActionResult SignUpWithGoogle()
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Home", null, Request.Url.Scheme);
            System.Diagnostics.Debug.WriteLine("Redirect URL: " + redirectUrl);

            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            HttpContext.GetOwinContext().Authentication.Challenge(properties, "Google");
            return new HttpUnauthorizedResult();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> ExternalLoginCallback()
        {
            System.Diagnostics.Debug.WriteLine("ExternalLoginCallback called.");
            System.Diagnostics.Debug.WriteLine($"Request URI: {Request.Url}");

            IOwinContext owinContext = HttpContext.GetOwinContext();
            if (owinContext == null)
            {
                System.Diagnostics.Debug.WriteLine("OWIN Context is null.");
                TempData["ErrorMessage"] = "Không thể truy cập OWIN Context. Vui lòng kiểm tra cấu hình OWIN.";
                return RedirectToAction("DangNhap", "Home");
            }

            var authManager = owinContext.Authentication;
            if (authManager == null)
            {
                System.Diagnostics.Debug.WriteLine("OWIN Authentication Manager is null.");
                TempData["ErrorMessage"] = "Không thể truy cập OWIN Authentication Manager.";
                return RedirectToAction("DangNhap", "Home");
            }

            var error = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(error))
            {
                System.Diagnostics.Debug.WriteLine($"Error in query string: {error}, Description: {Request.QueryString["error_description"] ?? "No description"}");
                TempData["ErrorMessage"] = error == "access_denied"
                    ? "Bạn đã hủy đăng nhập bằng Google. Vui lòng thử lại hoặc sử dụng phương thức khác."
                    : "Đăng nhập Google thất bại. Vui lòng thử lại.";
                return RedirectToAction("DangNhap", "Home");
            }

            System.Diagnostics.Debug.WriteLine("Calling GetExternalLoginInfoAsync...");
            var loginInfo = await authManager.GetExternalLoginInfoAsync();
            System.Diagnostics.Debug.WriteLine($"GetExternalLoginInfoAsync completed. LoginInfo is {(loginInfo == null ? "null" : "not null")}");

            if (loginInfo == null)
            {
                System.Diagnostics.Debug.WriteLine("External login info is null.");
                TempData["ErrorMessage"] = "Không thể lấy thông tin đăng nhập từ Google. Vui lòng thử lại.";
                return RedirectToAction("DangNhap", "Home");
            }

            System.Diagnostics.Debug.WriteLine($"LoginInfo details - Email: {loginInfo.Email ?? "Null"}, Provider: {loginInfo.Login?.LoginProvider ?? "Null"}, ProviderKey: {loginInfo.Login?.ProviderKey ?? "Null"}");

            if (string.IsNullOrEmpty(loginInfo.Email) || loginInfo.Login == null || string.IsNullOrEmpty(loginInfo.Login.LoginProvider) || string.IsNullOrEmpty(loginInfo.Login.ProviderKey))
            {
                System.Diagnostics.Debug.WriteLine("Missing required login information (Email, Provider, or ProviderKey).");
                TempData["ErrorMessage"] = "Không thể lấy thông tin đầy đủ từ Google. Vui lòng thử lại.";
                return RedirectToAction("DangNhap", "Home");
            }

            return await ProcessLoginInfo(loginInfo);
        }

        private async Task<ActionResult> ProcessLoginInfo(ExternalLoginInfo loginInfo)
        {
            System.Diagnostics.Debug.WriteLine("Processing login info.");
            var email = loginInfo.Email;
            var externalId = loginInfo.Login.ProviderKey;
            var name = loginInfo.DefaultUserName;

            System.Diagnostics.Debug.WriteLine($"Email: {email ?? "Null"}, ExternalId: {externalId ?? "Null"}, Name: {name ?? "Null"}");

            if (string.IsNullOrEmpty(email))
            {
                System.Diagnostics.Debug.WriteLine("Email is null or empty. Cannot proceed with login.");
                TempData["ErrorMessage"] = "Không thể lấy email từ Google. Vui lòng thử lại.";
                return RedirectToAction("DangNhap", "Home");
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Checking for existing user in database.");
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
                if (existingUser != null)
                {
                    System.Diagnostics.Debug.WriteLine("Existing user found: " + existingUser.Email);
                    SignInUser(existingUser);
                    return RedirectToRoleBasedPage(existingUser);
                }

                System.Diagnostics.Debug.WriteLine("No existing user found. Creating new user.");
                var newUser = new User
                {
                    Email = email,
                    ExternalId = externalId,
                    Provider = "Google",
                    FirstName = name?.Split(' ').FirstOrDefault() ?? "Unknown",
                    LastName = name?.Split(' ').LastOrDefault() ?? "User",
                    Birthday = DateTime.Now.AddYears(-18),
                    Gender = "Unknown",
                    Password = null,
                    Role = Role.User
                };

                System.Diagnostics.Debug.WriteLine("Adding new user to database.");
                _context.Users.Add(newUser);
                _context.SaveChanges();
                System.Diagnostics.Debug.WriteLine("User saved to database successfully. User ID: " + newUser.Id);

                System.Diagnostics.Debug.WriteLine("Signing in new user.");
                SignInUser(newUser);
                return RedirectToRoleBasedPage(newUser);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in ProcessLoginInfo: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                System.Diagnostics.Debug.WriteLine("Inner Exception: " + (ex.InnerException?.Message ?? "No inner exception"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đăng nhập Google. Vui lòng thử lại.";
                return RedirectToAction("DangNhap", "Home");
            }
        }

        private void SignInUser(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                
            };

            var identity = new ClaimsIdentity(claims, "ApplicationCookie");
            var authManager = HttpContext.GetOwinContext().Authentication;
            authManager.SignIn(new AuthenticationProperties { IsPersistent = false }, identity);
            HttpContext.Session["UserId"] = user.Id;
            HttpContext.Session["Email"] = user.Email;
            HttpContext.Session["Role"] = user.Role.ToString();
            HttpContext.Session["FullName"] = $"{user.FirstName} {user.LastName}";
            System.Diagnostics.Debug.WriteLine("Session stored: UserId=" + user.Id + ", Email=" + user.Email);
        }

        private ActionResult RedirectToRoleBasedPage(User user)
        {
            if (user.Role == Role.Admin)
            {
                return RedirectToAction("SanPham", "Admin");
            }
            return RedirectToAction("trangchu", "trangchu");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }

        [AllowAnonymous]
        public ActionResult Test()
        {
            System.Diagnostics.Debug.WriteLine("Test action called.");
            return Content("Kiểm tra thành công");
        }

        [AllowAnonymous]
        public ActionResult Error(string message)
        {
            ViewBag.ErrorMessage = message;
            return View();
        }
        public ActionResult DangXuat()
        {
            // Xóa cookie authentication
            var authManager = HttpContext.GetOwinContext().Authentication;
            authManager.SignOut("ApplicationCookie");

            // Xóa tất cả thông tin trong Session
            HttpContext.Session.Clear();
            HttpContext.Session.Abandon();

            return RedirectToAction("DangNhap", "Home");
        }
    }
}