using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;
using System.IO;
using System.Data.Entity;

namespace doanwebnangcao.Controllers
{
    [Authorize(Roles = "Admin")]
    public class Admincontroller : Controller
    {
        private readonly ApplicationDbContext _context;

        public Admincontroller()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Admin/SanPham
        public ActionResult SanPham()
        {
            var products = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .ToList();

            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .ToList();

            ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
            return View(products);
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        public ActionResult CreateProduct(Product product, HttpPostedFileBase ImageFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            // Kiểm tra danh mục con có tồn tại không
            var subcategory = _context.Subcategories.Find(product.SubcategoryId);
            if (subcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            // Kiểm tra mã sản phẩm đã tồn tại chưa
            if (_context.Products.Any(p => p.ProductCode == product.ProductCode))
            {
                TempData["ErrorMessage"] = "Mã sản phẩm đã tồn tại.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            // Xử lý upload file (không giới hạn định dạng)
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    // Kiểm tra và tạo thư mục nếu chưa tồn tại
                    var uploadDir = Server.MapPath("~/Uploads/Products");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName); // Giữ nguyên đuôi file
                    var path = Path.Combine(uploadDir, fileName);
                    ImageFile.SaveAs(path); // Lưu file vào thư mục
                    product.ImageUrl = "/Uploads/Products/" + fileName; // Lưu đường dẫn vào ImageUrl
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi upload file: " + ex.Message;
                    var products = _context.Products
                        .Include(p => p.Subcategory)
                        .Include(p => p.Subcategory.Category)
                        .ToList();
                    var subcategories = _context.Subcategories
                        .Include(s => s.Category)
                        .Where(s => s.IsActive)
                        .ToList();
                    ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                    return View("SanPham", products);
                }
            }

            _context.Products.Add(product);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("SanPham");
        }

        // POST: Admin/EditProduct
        [HttpPost]
        public ActionResult EditProduct(Product product, HttpPostedFileBase ImageFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            var existingProduct = _context.Products.Find(product.Id);
            if (existingProduct == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("SanPham");
            }

            // Kiểm tra danh mục con có tồn tại không
            var subcategory = _context.Subcategories.Find(product.SubcategoryId);
            if (subcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            // Kiểm tra mã sản phẩm đã tồn tại chưa (trừ sản phẩm hiện tại)
            if (_context.Products.Any(p => p.ProductCode == product.ProductCode && p.Id != product.Id))
            {
                TempData["ErrorMessage"] = "Mã sản phẩm đã tồn tại.";
                var products = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .ToList();
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .Where(s => s.IsActive)
                    .ToList();
                ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                return View("SanPham", products);
            }

            // Xử lý upload file nếu có (không giới hạn định dạng)
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    // Kiểm tra và tạo thư mục nếu chưa tồn tại
                    var uploadDir = Server.MapPath("~/Uploads/Products");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        var oldImagePath = Server.MapPath(existingProduct.ImageUrl);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName); // Giữ nguyên đuôi file
                    var path = Path.Combine(uploadDir, fileName);
                    ImageFile.SaveAs(path); // Lưu file mới
                    existingProduct.ImageUrl = "/Uploads/Products/" + fileName; // Cập nhật đường dẫn
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi upload file: " + ex.Message;
                    var products = _context.Products
                        .Include(p => p.Subcategory)
                        .Include(p => p.Subcategory.Category)
                        .ToList();
                    var subcategories = _context.Subcategories
                        .Include(s => s.Category)
                        .Where(s => s.IsActive)
                        .ToList();
                    ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
                    return View("SanPham", products);
                }
            }

            // Cập nhật thông tin sản phẩm
            existingProduct.SubcategoryId = product.SubcategoryId;
            existingProduct.ProductCode = product.ProductCode;
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Description = product.Description;
            existingProduct.IsActive = product.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("SanPham");
        }

        // GET: Admin/DeleteProduct
        public ActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("SanPham");
            }

            // Kiểm tra xem sản phẩm có liên quan đến các bảng khác không
            if (_context.OrderDetails.Any(od => od.ProductId == id) ||
                _context.CartDetails.Any(cd => cd.ProductId == id) ||
                _context.Wishlists.Any(w => w.ProductId == id) ||
                _context.Reviews.Any(r => r.ProductId == id) ||
                _context.ProductImages.Any(pi => pi.ProductId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì vẫn còn dữ liệu liên quan (đơn hàng, giỏ hàng, danh sách yêu thích, đánh giá, hình ảnh).";
                return RedirectToAction("SanPham");
            }

            // Xóa file nếu có
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var imagePath = Server.MapPath(product.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("SanPham");
        }

        // GET: Admin/danhmuc
        public ActionResult danhmuc()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        public ActionResult CreateCategory(Category category)
        {
            if (!ModelState.IsValid)
            {
                var categories = _context.Categories.ToList();
                return View("danhmuc", categories);
            }

            // Kiểm tra tên danh mục đã tồn tại chưa
            if (_context.Categories.Any(c => c.Name == category.Name))
            {
                TempData["ErrorMessage"] = "Tên danh mục đã tồn tại.";
                var categories = _context.Categories.ToList();
                return View("danhmuc", categories);
            }

            category.IsActive = category.IsActive;
            _context.Categories.Add(category);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm danh mục thành công!";
            return RedirectToAction("danhmuc");
        }

        // POST: Admin/EditCategory
        [HttpPost]
        public ActionResult EditCategory(Category category)
        {
            if (!ModelState.IsValid)
            {
                var categories = _context.Categories.ToList();
                return View("danhmuc", categories);
            }

            var existingCategory = _context.Categories.Find(category.Id);
            if (existingCategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại.";
                return RedirectToAction("danhmuc");
            }

            // Kiểm tra tên danh mục đã tồn tại chưa (trừ danh mục hiện tại)
            if (_context.Categories.Any(c => c.Name == category.Name && c.Id != category.Id))
            {
                TempData["ErrorMessage"] = "Tên danh mục đã tồn tại.";
                var categories = _context.Categories.ToList();
                return View("danhmuc", categories);
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.IsActive = category.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("danhmuc");
        }

        // GET: Admin/DeleteCategory
        public ActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại.";
                return RedirectToAction("danhmuc");
            }

            // Kiểm tra xem danh mục có danh mục con không
            if (_context.Subcategories.Any(s => s.CategoryId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục vì vẫn còn danh mục con liên quan.";
                return RedirectToAction("danhmuc");
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction("danhmuc");
        }

        // GET: Admin/danhmucon
        public ActionResult danhmucon()
        {
            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .ToList();
            var categories = _context.Categories.Where(c => c.IsActive).ToList();
            ViewBag.Categories = categories ?? new List<Category>();
            return View(subcategories);
        }

        // POST: Admin/CreateSubcategory
        [HttpPost]
        public ActionResult CreateSubcategory(Subcategory subcategory)
        {
            if (!ModelState.IsValid)
            {
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            // Kiểm tra danh mục cha có tồn tại không
            var category = _context.Categories.Find(subcategory.CategoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Danh mục cha không tồn tại.";
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            // Kiểm tra tên danh mục con đã tồn tại trong cùng danh mục cha chưa
            if (_context.Subcategories.Any(s => s.Name == subcategory.Name && s.CategoryId == subcategory.CategoryId))
            {
                TempData["ErrorMessage"] = "Tên danh mục con đã tồn tại trong danh mục cha này.";
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            subcategory.IsActive = subcategory.IsActive;
            _context.Subcategories.Add(subcategory);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm danh mục con thành công!";
            return RedirectToAction("danhmucon");
        }

        // POST: Admin/EditSubcategory
        [HttpPost]
        public ActionResult EditSubcategory(Subcategory subcategory)
        {
            if (!ModelState.IsValid)
            {
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            var existingSubcategory = _context.Subcategories.Find(subcategory.Id);
            if (existingSubcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                return RedirectToAction("danhmucon");
            }

            // Kiểm tra danh mục cha có tồn tại không
            var category = _context.Categories.Find(subcategory.CategoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Danh mục cha không tồn tại.";
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            // Kiểm tra tên danh mục con đã tồn tại trong cùng danh mục cha chưa (trừ danh mục con hiện tại)
            if (_context.Subcategories.Any(s => s.Name == subcategory.Name && s.CategoryId == subcategory.CategoryId && s.Id != subcategory.Id))
            {
                TempData["ErrorMessage"] = "Tên danh mục con đã tồn tại trong danh mục cha này.";
                var subcategories = _context.Subcategories
                    .Include(s => s.Category)
                    .ToList();
                var categories = _context.Categories.Where(c => c.IsActive).ToList();
                ViewBag.Categories = categories ?? new List<Category>();
                return View("danhmucon", subcategories);
            }

            existingSubcategory.CategoryId = subcategory.CategoryId;
            existingSubcategory.Name = subcategory.Name;
            existingSubcategory.Description = subcategory.Description;
            existingSubcategory.IsActive = subcategory.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật danh mục con thành công!";
            return RedirectToAction("danhmucon");
        }

        // GET: Admin/DeleteSubcategory
        public ActionResult DeleteSubcategory(int id)
        {
            var subcategory = _context.Subcategories.Find(id);
            if (subcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                return RedirectToAction("danhmucon");
            }

            // Kiểm tra xem danh mục con có sản phẩm liên quan không
            if (_context.Products.Any(p => p.SubcategoryId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục con vì vẫn còn sản phẩm liên quan.";
                return RedirectToAction("danhmucon");
            }

            _context.Subcategories.Remove(subcategory);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa danh mục con thành công!";
            return RedirectToAction("danhmucon");
        }
    }
}