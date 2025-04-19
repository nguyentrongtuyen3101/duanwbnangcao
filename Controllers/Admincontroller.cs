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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Admin/SanPham
        public ActionResult SanPham()
        {
            ViewBag.ActivePage = "SanPham";
            var products = _context.Products
                .Include("Subcategory")
                .Include("Subcategory.Category")
                .Include("ProductVariants.ProductImages")
                .Include("ProductImages")
                .ToList();

            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .ToList();

            var sizes = _context.Sizes.Where(s => s.IsActive).ToList();
            var colors = _context.Colors.Where(c => c.IsActive).ToList();

            if (!sizes.Any())
            {
                TempData["WarningMessage"] = "Chưa có kích thước nào được thiết lập. Vui lòng thêm kích thước trước.";
            }
            if (!colors.Any())
            {
                TempData["WarningMessage"] = TempData["WarningMessage"]?.ToString() + " Chưa có màu sắc nào được thiết lập. Vui lòng thêm màu sắc trước.";
            }

            ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
            ViewBag.Sizes = sizes ?? new List<Size>();
            ViewBag.Colors = colors ?? new List<Color>();
            return View(products);
        }

        // GET: Admin/SanPhamBienThe
        public ActionResult SanPhamBienThe(int productId)
        {
            ViewBag.ActivePage = "SanPham";
            var product = _context.Products
            .Include("Subcategory")
            .Include("Subcategory.Category")
            .Include("ProductVariants.ProductImages") // Load ProductImages cho từng ProductVariant
            .Include("ProductImages")
            .SingleOrDefault(p => p.Id == productId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("SanPham");
            }

            var sizes = _context.Sizes.Where(s => s.IsActive).ToList();
            var colors = _context.Colors.Where(c => c.IsActive).ToList();

            ViewBag.Sizes = sizes ?? new List<Size>();
            ViewBag.Colors = colors ?? new List<Color>();
            return View("sanphambienthe", product);
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        public ActionResult CreateProduct(Product product, HttpPostedFileBase ImageFile)
        {
            ViewBag.ActivePage = "SanPham";

            if (!ModelState.IsValid)
            {
                // Hiển thị lỗi chi tiết từ ModelState
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "Không thể thêm sản phẩm: " + string.Join(", ", errors);
                return LoadSanPhamView();
            }

            // Kiểm tra danh mục con
            var subcategory = _context.Subcategories.Find(product.SubcategoryId);
            if (subcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                return LoadSanPhamView();
            }

            // Kiểm tra mã sản phẩm
            if (_context.Products.Any(p => p.ProductCode == product.ProductCode))
            {
                TempData["ErrorMessage"] = "Mã sản phẩm đã tồn tại.";
                return LoadSanPhamView();
            }

            // Xử lý ảnh chính (nếu có)
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    var uploadDir = Server.MapPath("~/Uploads/Products");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var path = Path.Combine(uploadDir, fileName);
                    ImageFile.SaveAs(path);
                    product.ImageUrl = "/Uploads/Products/" + fileName;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi upload ảnh: " + ex.Message;
                    return LoadSanPhamView();
                }
            }

            // Đặt StockQuantity mặc định là 0 vì chưa có biến thể
            product.StockQuantity = 0;
            product.CreatedAt = DateTime.Now;

            _context.Products.Add(product);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi lưu sản phẩm vào database: " + ex.Message;
                return LoadSanPhamView();
            }

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công! Vui lòng chỉnh sửa để thêm biến thể.";
            return RedirectToAction("SanPham");
        }

        // GET: Admin/EditProduct
        public ActionResult EditProduct(int id)
        {
            ViewBag.ActivePage = "SanPham";
            var product = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .SingleOrDefault(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("SanPham");
            }

            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .ToList();
            var sizes = _context.Sizes.Where(s => s.IsActive).ToList();
            var colors = _context.Colors.Where(c => c.IsActive).ToList();

            ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
            ViewBag.Sizes = sizes ?? new List<Size>();
            ViewBag.Colors = colors ?? new List<Color>();
            return View(product);
        }

        [HttpPost]
        public ActionResult EditProduct(Product product, HttpPostedFileBase ImageFile)
        {
            ViewBag.ActivePage = "SanPham";
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
                return LoadSanPhamView();
            }

            var existingProduct = _context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .SingleOrDefault(p => p.Id == product.Id);

            if (existingProduct == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return LoadSanPhamView();
            }

            // Kiểm tra danh mục con
            var subcategory = _context.Subcategories.Find(product.SubcategoryId);
            if (subcategory == null)
            {
                TempData["ErrorMessage"] = "Danh mục con không tồn tại.";
                return LoadSanPhamView();
            }

            // Kiểm tra mã sản phẩm
            if (_context.Products.Any(p => p.ProductCode == product.ProductCode && p.Id != product.Id))
            {
                TempData["ErrorMessage"] = "Mã sản phẩm đã tồn tại.";
                return LoadSanPhamView();
            }

            // Cập nhật thông tin cơ bản
            existingProduct.SubcategoryId = product.SubcategoryId;
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.DiscountedPrice = product.DiscountedPrice;
            existingProduct.ProductCode = product.ProductCode;
            existingProduct.IsActive = product.IsActive;

            // Xử lý ảnh chính (nếu có)
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    var uploadDir = Server.MapPath("~/Uploads/Products");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var path = Path.Combine(uploadDir, fileName);
                    ImageFile.SaveAs(path);
                    existingProduct.ImageUrl = "/Uploads/Products/" + fileName;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi upload ảnh: " + ex.Message;
                    return LoadSanPhamView();
                }
            }

            // Cập nhật tổng StockQuantity (tính lại từ các biến thể hiện có)
            existingProduct.StockQuantity = existingProduct.ProductVariants?.Sum(v => v.StockQuantity) ?? 0;

            // Lưu thay đổi
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi lưu sản phẩm: " + ex.Message;
                return LoadSanPhamView();
            }

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("SanPham");
        }

        [HttpPost]
        public JsonResult AddProductVariant(int productId, int sizeId, int colorId, int stockQuantity, HttpPostedFileBase variantImageFile)
        {
            try
            {
                var product = _context.Products
                    .Include(p => p.ProductVariants)
                    .SingleOrDefault(p => p.Id == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Kiểm tra kích thước
                if (!_context.Sizes.Any(s => s.Id == sizeId))
                {
                    return Json(new { success = false, message = $"Kích thước với ID {sizeId} không tồn tại." });
                }

                // Kiểm tra màu sắc
                // Kiểm tra màu sắc
                if (!_context.Colors.Any(c => c.Id == colorId))
                {
                    return Json(new { success = false, message = $"Màu sắc với ID {colorId} không tồn tại." });
                }

                // Kiểm tra xem biến thể với SizeId và ColorId này đã tồn tại chưa
                if (product.ProductVariants.Any(v => v.SizeId == sizeId && v.ColorId == colorId))
                {
                    return Json(new { success = false, message = "Biến thể với kích thước và màu sắc này đã tồn tại." });
                }

                // Tạo biến thể mới
                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    SizeId = sizeId,
                    ColorId = colorId,
                    StockQuantity = stockQuantity,
                    VariantPrice = product.Price, // Giá biến thể lấy theo giá mặc định của sản phẩm
                    IsActive = true
                };

                // Xử lý ảnh của biến thể (nếu có)
                string variantImageUrl = null;
                if (variantImageFile != null && variantImageFile.ContentLength > 0)
                {
                    try
                    {
                        var uploadDir = Server.MapPath("~/Uploads/Products");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(variantImageFile.FileName);
                        var path = Path.Combine(uploadDir, fileName);
                        variantImageFile.SaveAs(path);
                        variantImageUrl = "/Uploads/Products/" + fileName;

                        // Lưu đường dẫn ảnh vào ProductVariant
                        variant.VariantImageUrl = variantImageUrl; // Thêm dòng này
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Lỗi khi upload ảnh biến thể: " + ex.Message });
                    }
                }

                // Thêm biến thể vào database
                _context.ProductVariants.Add(variant);
                _context.SaveChanges();

                // Thêm ảnh cho biến thể (nếu có) vào bảng ProductImages
                if (variantImageUrl != null)
                {
                    var productImage = new ProductImage
                    {
                        ProductVariantId = variant.Id,
                        ImageUrl = variantImageUrl,
                        IsMain = true
                    };
                    _context.ProductImages.Add(productImage);
                    _context.SaveChanges();
                }

                // Cập nhật tổng StockQuantity của sản phẩm
                product.StockQuantity = product.ProductVariants.Sum(v => v.StockQuantity);
                _context.SaveChanges();

                // Trả về thông tin biến thể mới để cập nhật giao diện
                return Json(new
                {
                    success = true,
                    variant = new
                    {
                        variant.Id,
                        variant.SizeId,
                        variant.ColorId,
                        variant.StockQuantity,
                        variant.VariantPrice,
                        VariantImageUrl = variant.VariantImageUrl, // Sử dụng variant.VariantImageUrl
                        variant.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm biến thể: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteProductVariant(int variantId)
        {
            try
            {
                var variant = _context.ProductVariants
                    .Include(v => v.ProductImages)
                    .SingleOrDefault(v => v.Id == variantId);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Biến thể không tồn tại." });
                }

                var product = _context.Products
                    .Include(p => p.ProductVariants)
                    .SingleOrDefault(p => p.Id == variant.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Xóa ảnh liên quan đến biến thể
                if (variant.ProductImages != null)
                {
                    _context.ProductImages.RemoveRange(variant.ProductImages);
                    variant.VariantImageUrl = null; // Đặt lại VariantImageUrl
                }

                // Xóa biến thể
                _context.ProductVariants.Remove(variant);

                // Cập nhật tổng StockQuantity của sản phẩm
                product.StockQuantity = product.ProductVariants.Sum(v => v.StockQuantity);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa biến thể thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa biến thể: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditProductVariant(int variantId, int sizeId, int colorId, int stockQuantity, HttpPostedFileBase variantImageFile)
        {
            try
            {
                var variant = _context.ProductVariants
                    .Include(v => v.ProductImages)
                    .SingleOrDefault(v => v.Id == variantId);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Biến thể không tồn tại." });
                }

                var product = _context.Products
                    .Include(p => p.ProductVariants)
                    .SingleOrDefault(p => p.Id == variant.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                if (!_context.Sizes.Any(s => s.Id == sizeId))
                {
                    return Json(new { success = false, message = $"Kích thước với ID {sizeId} không tồn tại." });
                }

                if (!_context.Colors.Any(c => c.Id == colorId))
                {
                    return Json(new { success = false, message = $"Màu sắc với ID {colorId} không tồn tại." });
                }

                if (product.ProductVariants.Any(v => v.SizeId == sizeId && v.ColorId == colorId && v.Id != variantId))
                {
                    return Json(new { success = false, message = "Biến thể với kích thước và màu sắc này đã tồn tại." });
                }

                variant.SizeId = sizeId;
                variant.ColorId = colorId;
                variant.StockQuantity = stockQuantity;

                if (variantImageFile != null && variantImageFile.ContentLength > 0)
                {
                    try
                    {
                        var uploadDir = Server.MapPath("~/Uploads/Products");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(variantImageFile.FileName);
                        var path = Path.Combine(uploadDir, fileName);
                        variantImageFile.SaveAs(path);
                        var newImageUrl = "/Uploads/Products/" + fileName;

                        if (variant.ProductImages != null && variant.ProductImages.Any())
                        {
                            _context.ProductImages.RemoveRange(variant.ProductImages);
                        }

                        variant.VariantImageUrl = newImageUrl;

                        var productImage = new ProductImage
                        {
                            ProductVariantId = variant.Id,
                            ImageUrl = newImageUrl,
                            IsMain = true
                        };
                        _context.ProductImages.Add(productImage);
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = "Lỗi khi upload ảnh biến thể: " + ex.Message });
                    }
                }

                _context.SaveChanges();

                product.StockQuantity = product.ProductVariants.Sum(v => v.StockQuantity);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    variant = new
                    {
                        variant.Id,
                        variant.SizeId,
                        variant.ColorId,
                        variant.StockQuantity,
                        variant.VariantPrice,
                        VariantImageUrl = variant.VariantImageUrl,
                        variant.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi chỉnh sửa biến thể: " + ex.Message });
            }
        }

        // GET: Admin/DeleteProduct
        public ActionResult DeleteProduct(int id)
        {
            ViewBag.ActivePage = "SanPham";
            var product = _context.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .SingleOrDefault(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("SanPham");
            }

            // Kiểm tra xem sản phẩm có liên quan đến giỏ hàng hoặc đơn hàng không
            if (_context.CartDetails.Any(cd => cd.ProductVariant.ProductId == id) ||
                _context.OrderDetails.Any(od => od.ProductVariant.ProductId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì sản phẩm đang được sử dụng trong giỏ hàng hoặc đơn hàng.";
                return RedirectToAction("SanPham");
            }

            // Xóa các biến thể liên quan
            if (product.ProductVariants != null && product.ProductVariants.Any())
            {
                foreach (var variant in product.ProductVariants.ToList())
                {
                    _context.ProductVariants.Remove(variant);
                }
            }

            // Xóa các hình ảnh liên quan
            if (product.ProductImages != null && product.ProductImages.Any())
            {
                foreach (var image in product.ProductImages.ToList())
                {
                    _context.ProductImages.Remove(image);
                }
            }

            // Xóa sản phẩm
            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("SanPham");
        }

        // GET: Admin/Sizes
        public ActionResult Sizes()
        {
            ViewBag.ActivePage = "Sizes";
            var sizes = _context.Sizes.ToList();
            return View(sizes);
        }

        // POST: Admin/CreateSize
        [HttpPost]
        public ActionResult CreateSize(Size size)
        {
            ViewBag.ActivePage = "Sizes";
            if (!ModelState.IsValid)
            {
                var sizes = _context.Sizes.ToList();
                return View("Sizes", sizes);
            }

            if (_context.Sizes.Any(s => s.Name == size.Name))
            {
                TempData["ErrorMessage"] = "Tên kích thước đã tồn tại.";
                var sizes = _context.Sizes.ToList();
                return View("Sizes", sizes);
            }

            size.IsActive = true;
            _context.Sizes.Add(size);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm kích thước thành công!";
            return RedirectToAction("Sizes");
        }

        // POST: Admin/EditSize
        [HttpPost]
        public ActionResult EditSize(Size size)
        {
            ViewBag.ActivePage = "Sizes";
            if (!ModelState.IsValid)
            {
                var sizes = _context.Sizes.ToList();
                return View("Sizes", sizes);
            }

            var existingSize = _context.Sizes.Find(size.Id);
            if (existingSize == null)
            {
                TempData["ErrorMessage"] = "Kích thước không tồn tại.";
                return RedirectToAction("Sizes");
            }

            if (_context.Sizes.Any(s => s.Name == size.Name && s.Id != size.Id))
            {
                TempData["ErrorMessage"] = "Tên kích thước đã tồn tại.";
                var sizes = _context.Sizes.ToList();
                return View("Sizes", sizes);
            }

            existingSize.Name = size.Name;
            existingSize.Description = size.Description;
            existingSize.IsActive = size.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật kích thước thành công!";
            return RedirectToAction("Sizes");
        }

        // GET: Admin/DeleteSize
        public ActionResult DeleteSize(int id)
        {
            ViewBag.ActivePage = "Sizes";
            var size = _context.Sizes.Find(id);
            if (size == null)
            {
                TempData["ErrorMessage"] = "Kích thước không tồn tại.";
                return RedirectToAction("Sizes");
            }

            if (_context.ProductVariants.Any(pv => pv.SizeId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa kích thước vì vẫn còn biến thể sản phẩm liên quan.";
                return RedirectToAction("Sizes");
            }

            _context.Sizes.Remove(size);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa kích thước thành công!";
            return RedirectToAction("Sizes");
        }

        // GET: Admin/Colors
        public ActionResult Colors()
        {
            ViewBag.ActivePage = "Colors";
            var colors = _context.Colors.ToList();
            return View(colors);
        }

        // POST: Admin/CreateColor
        [HttpPost]
        public ActionResult CreateColor(Color color)
        {
            ViewBag.ActivePage = "Colors";
            if (!ModelState.IsValid)
            {
                var colors = _context.Colors.ToList();
                return View("Colors", colors);
            }

            if (_context.Colors.Any(c => c.Name == color.Name))
            {
                TempData["ErrorMessage"] = "Tên màu sắc đã tồn tại.";
                var colors = _context.Colors.ToList();
                return View("Colors", colors);
            }

            color.IsActive = true;
            _context.Colors.Add(color);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm màu sắc thành công!";
            return RedirectToAction("Colors");
        }

        // POST: Admin/EditColor
        [HttpPost]
        public ActionResult EditColor(Color color)
        {
            ViewBag.ActivePage = "Colors";
            if (!ModelState.IsValid)
            {
                var colors = _context.Colors.ToList();
                return View("Colors", colors);
            }

            var existingColor = _context.Colors.Find(color.Id);
            if (existingColor == null)
            {
                TempData["ErrorMessage"] = "Màu sắc không tồn tại.";
                return RedirectToAction("Colors");
            }

            if (_context.Colors.Any(c => c.Name == color.Name && c.Id != color.Id))
            {
                TempData["ErrorMessage"] = "Tên màu sắc đã tồn tại.";
                var colors = _context.Colors.ToList();
                return View("Colors", colors);
            }

            existingColor.Name = color.Name;
            existingColor.Description = color.Description;
            existingColor.IsActive = color.IsActive;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật màu sắc thành công!";
            return RedirectToAction("Colors");
        }

        // GET: Admin/DeleteColor
        public ActionResult DeleteColor(int id)
        {
            ViewBag.ActivePage = "Colors";
            var color = _context.Colors.Find(id);
            if (color == null)
            {
                TempData["ErrorMessage"] = "Màu sắc không tồn tại.";
                return RedirectToAction("Colors");
            }

            if (_context.ProductVariants.Any(pv => pv.ColorId == id))
            {
                TempData["ErrorMessage"] = "Không thể xóa màu sắc vì vẫn còn biến thể sản phẩm liên quan.";
                return RedirectToAction("Colors");
            }

            _context.Colors.Remove(color);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Xóa màu sắc thành công!";
            return RedirectToAction("Colors");
        }

        // Hàm hỗ trợ tải lại view SanPham
        private ActionResult LoadSanPhamView()
        {
            var products = _context.Products
                .Include("Subcategory")
                .Include("Subcategory.Category")
                .Include("ProductVariants.ProductImages")
                .Include("ProductImages")
                .ToList();
            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .ToList();
            var sizes = _context.Sizes.Where(s => s.IsActive).ToList();
            var colors = _context.Colors.Where(c => c.IsActive).ToList();

            ViewBag.Subcategories = subcategories ?? new List<Subcategory>();
            ViewBag.Sizes = sizes ?? new List<Size>();
            ViewBag.Colors = colors ?? new List<Color>();
            return View("SanPham", products);
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