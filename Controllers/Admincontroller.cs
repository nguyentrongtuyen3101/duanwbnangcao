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
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
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

        // POST: Admin/CreateProduct
        [HttpPost]
        public ActionResult CreateProduct(Product product, HttpPostedFileBase ImageFile)
        {
            ViewBag.ActivePage = "SanPham";
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
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
            _context.SaveChanges();

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

        // POST: Admin/EditProduct
        [HttpPost]
        public ActionResult EditProduct(Product product, HttpPostedFileBase ImageFile, int[] VariantIds, int[] SizeIds, int[] ColorIds, int[] VariantQuantities, decimal[] VariantPrices, HttpPostedFileBase[] VariantImageFiles)
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

            // Xử lý ProductVariants
            var newVariants = new List<ProductVariant>();
            if (SizeIds != null && ColorIds != null && VariantQuantities != null)
            {
                for (int i = 0; i < SizeIds.Length; i++)
                {
                    var variantId = VariantIds != null && i < VariantIds.Length ? VariantIds[i] : 0;
                    var variant = existingProduct.ProductVariants?.FirstOrDefault(v => v.Id == variantId) ?? new ProductVariant
                    {
                        ProductId = existingProduct.Id
                    };

                    variant.SizeId = SizeIds[i];
                    variant.ColorId = ColorIds[i];
                    variant.StockQuantity = VariantQuantities[i];
                    variant.VariantPrice = VariantPrices != null && i < VariantPrices.Length ? (decimal?)VariantPrices[i] : null; // Sửa dòng này
                    variant.IsActive = true;

                    // Xử lý ảnh của biến thể
                    if (VariantImageFiles != null && i < VariantImageFiles.Length && VariantImageFiles[i] != null && VariantImageFiles[i].ContentLength > 0)
                    {
                        try
                        {
                            var uploadDir = Server.MapPath("~/Uploads/Products");
                            if (!Directory.Exists(uploadDir))
                            {
                                Directory.CreateDirectory(uploadDir);
                            }
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(VariantImageFiles[i].FileName);
                            var path = Path.Combine(uploadDir, fileName);
                            VariantImageFiles[i].SaveAs(path);
                            variant.VariantImageUrl = "/Uploads/Products/" + fileName;

                            // Thêm vào ProductImage
                            var productImage = new ProductImage
                            {
                                ProductVariantId = variant.Id, // Sẽ cập nhật sau khi lưu variant
                                ImageUrl = "/Uploads/Products/" + fileName,
                                IsMain = true
                            };
                            existingProduct.ProductImages.Add(productImage);
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = "Lỗi khi upload ảnh biến thể: " + ex.Message;
                            return LoadSanPhamView();
                        }
                    }

                    if (variant.Id == 0)
                    {
                        _context.ProductVariants.Add(variant);
                    }
                    newVariants.Add(variant);
                }
            }

            // Xóa các variant không còn trong danh sách
            if (existingProduct.ProductVariants != null)
            {
                var variantsToRemove = existingProduct.ProductVariants
                    .Where(v => !newVariants.Any(nv => nv.Id == v.Id && v.Id != 0))
                    .ToList();
                foreach (var variant in variantsToRemove)
                {
                    _context.ProductVariants.Remove(variant);
                    var relatedImages = _context.ProductImages.Where(pi => pi.ProductVariantId == variant.Id).ToList();
                    _context.ProductImages.RemoveRange(relatedImages);
                }
            }

            // Cập nhật tổng StockQuantity
            existingProduct.StockQuantity = newVariants.Sum(v => v.StockQuantity);
            existingProduct.ProductVariants = newVariants;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
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
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
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
    }
}