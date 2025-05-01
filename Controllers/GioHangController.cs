using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;
using System.Data.Entity;

namespace doanwebnangcao.Controllers
{
    public class GioHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GioHangController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: GioHang/giohang
        public ActionResult giohang()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = (int)Session["UserId"];

            var cart = _context.Carts
                .Include(c => c.CartDetails)
                .Include(c => c.CartDetails.Select(cd => cd.ProductVariant))
                .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Product))
                .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Size))
                .Include(c => c.CartDetails.Select(cd => cd.ProductVariant.Color))
                .SingleOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    CartDetails = new List<CartDetail>()
                };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            return View(cart);
        }

        // POST: GioHang/AddToCart
        [HttpPost]
        public ActionResult AddToCart(int productId, int variantId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
            }

            int userId = (int)Session["UserId"];

            var productVariant = _context.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.Size)
                .Include(pv => pv.Color)
                .SingleOrDefault(pv => pv.Id == variantId && pv.ProductId == productId && pv.IsActive);

            if (productVariant == null)
            {
                return Json(new { success = false, message = "Biến thể sản phẩm không tồn tại." });
            }

            if (productVariant.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "Số lượng trong kho không đủ." });
            }

            var cart = _context.Carts
                .Include(c => c.CartDetails)
                .SingleOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    CartDetails = new List<CartDetail>()
                };
                _context.Carts.Add(cart);
            }

            var cartDetail = cart.CartDetails
                .SingleOrDefault(cd => cd.ProductVariantId == variantId);

            if (cartDetail != null)
            {
                cartDetail.Quantity += quantity;
                cartDetail.UnitPrice = productVariant.VariantDiscountedPrice ?? productVariant.VariantPrice ?? productVariant.Product.DiscountedPrice ?? productVariant.Product.Price;
            }
            else
            {
                cartDetail = new CartDetail
                {
                    CartId = cart.Id,
                    ProductVariantId = variantId,
                    Quantity = quantity,
                    UnitPrice = productVariant.VariantDiscountedPrice ?? productVariant.VariantPrice ?? productVariant.Product.DiscountedPrice ?? productVariant.Product.Price
                };
                cart.CartDetails.Add(cartDetail);
            }

            _context.SaveChanges();

            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng." });
        }

        // POST: GioHang/UpdateQuantity
        [HttpPost]
        public ActionResult UpdateQuantity(int cartDetailId, int quantity)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để cập nhật giỏ hàng." });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
            }

            int userId = (int)Session["UserId"];
            var cartDetail = _context.CartDetails
                .Include(cd => cd.ProductVariant)
                .SingleOrDefault(cd => cd.Id == cartDetailId && cd.Cart.UserId == userId);

            if (cartDetail == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }

            if (cartDetail.ProductVariant.StockQuantity < quantity)
            {
                return Json(new { success = false, message = "Số lượng trong kho không đủ." });
            }

            cartDetail.Quantity = quantity;
            _context.SaveChanges();

            return Json(new { success = true, message = "Đã cập nhật số lượng." });
        }

        // POST: GioHang/RemoveFromCart
        [HttpPost]
        public ActionResult RemoveFromCart(int cartDetailId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa sản phẩm khỏi giỏ hàng." });
            }

            int userId = (int)Session["UserId"];
            var cartDetail = _context.CartDetails
                .SingleOrDefault(cd => cd.Id == cartDetailId && cd.Cart.UserId == userId);

            if (cartDetail == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }

            _context.CartDetails.Remove(cartDetail);
            _context.SaveChanges();

            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng." });
        }

        // POST: GioHang/ClearCart
        [HttpPost]
        public ActionResult ClearCart()
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa giỏ hàng." });
            }

            int userId = (int)Session["UserId"];
            var cart = _context.Carts
                .Include(c => c.CartDetails)
                .SingleOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.CartDetails.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng đã trống." });
            }

            _context.CartDetails.RemoveRange(cart.CartDetails);
            _context.SaveChanges();

            return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng." });
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