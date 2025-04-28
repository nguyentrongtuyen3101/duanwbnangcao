using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;

namespace doanwebnangcao.Controllers
{
    public class yeuthichController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: yeuthich/Wishlist
        public ActionResult Wishlist()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];
            var wishlist = db.Wishlists
                .Where(w => w.UserId == userId)
                .ToList();

            return View(wishlist);
        }

        // POST: yeuthich/AddToWishlist
        [HttpPost]
        public ActionResult AddToWishlist(int productId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào danh sách yêu thích." });
            }

            int userId = (int)Session["UserId"];
            var existingWishlistItem = db.Wishlists
                .FirstOrDefault(w => w.UserId == userId && w.ProductId == productId);

            if (existingWishlistItem != null)
            {
                return Json(new { success = false, message = "Sản phẩm này đã có trong danh sách yêu thích." });
            }

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                AddedDate = DateTime.Now
            };

            db.Wishlists.Add(wishlistItem);
            db.SaveChanges();

            return Json(new { success = true, message = "Đã thêm sản phẩm vào danh sách yêu thích." });
        }

        // GET: yeuthich/Remove/5
        public ActionResult Remove(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            var wishlistItem = db.Wishlists.Find(id);
            if (wishlistItem == null || wishlistItem.UserId != (int)Session["UserId"])
            {
                return HttpNotFound();
            }

            db.Wishlists.Remove(wishlistItem);
            db.SaveChanges();

            return RedirectToAction("Wishlist");
        }

        // GET: yeuthich/ClearWishlist
        public ActionResult ClearWishlist()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];
            var wishlistItems = db.Wishlists
                .Where(w => w.UserId == userId)
                .ToList();

            db.Wishlists.RemoveRange(wishlistItems);
            db.SaveChanges();

            return RedirectToAction("Wishlist");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}