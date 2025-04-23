using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using doanwebnangcao.Models;
using System.Data.Entity;

namespace doanwebnangcao.Controllers
{
    public class HangMoiVeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HangMoiVeController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: HangMoiVe/hangmoive
        public ActionResult hangmoive(int? categoryId, int? subcategoryId, decimal? minPrice, decimal? maxPrice, int? colorId, int? sizeId, string sortBy = "Newest", int page = 1, int pageSize = 12)
        {
            // Lấy danh sách danh mục và danh mục con
            var categories = _context.Categories
                .Where(c => c.IsActive)
                .ToList();
            var subcategories = _context.Subcategories
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .ToList();
            var colors = _context.Colors
                .Where(c => c.IsActive)
                .ToList();
            var sizes = _context.Sizes
                .Where(s => s.IsActive)
                .ToList();

            // Lấy 3 danh mục đầu tiên cho "Tìm kiếm liên quan"
            var relatedCategories = categories.Take(3).ToList();

            // Tính số lượng sản phẩm cho mỗi danh mục và danh mục con
            var categoryProductCounts = categories.ToDictionary(
                c => c.Id,
                c => _context.Products.Count(p => p.Subcategory.CategoryId == c.Id && p.IsActive)
            );
            var subcategoryProductCounts = subcategories.ToDictionary(
                s => s.Id,
                s => _context.Products.Count(p => p.SubcategoryId == s.Id && p.IsActive)
            );
            var colorVariantCounts = colors.ToDictionary(
                c => c.Id,
                c => _context.ProductVariants.Count(pv => pv.ColorId == c.Id && pv.IsActive)
            );
            var sizeVariantCounts = sizes.ToDictionary(
                s => s.Id,
                s => _context.ProductVariants.Count(pv => pv.SizeId == s.Id && pv.IsActive)
            );
            var ratingCounts = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
            {
                ratingCounts[i] = _context.Products.Count(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= i && p.IsActive);
            }

            // Truy vấn sản phẩm
            var productsQuery = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive);

            // Lọc theo Category
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Subcategory.CategoryId == categoryId.Value);
            }

            // Lọc theo Subcategory
            if (subcategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.SubcategoryId == subcategoryId.Value);
            }

            // Lọc theo giá
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.DiscountedPrice.HasValue ? p.DiscountedPrice >= minPrice.Value : p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.DiscountedPrice.HasValue ? p.DiscountedPrice <= maxPrice.Value : p.Price <= maxPrice.Value);
            }

            // Lọc theo màu sắc
            if (colorId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductVariants.Any(v => v.ColorId == colorId.Value && v.IsActive));
            }

            // Lọc theo kích thước
            if (sizeId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.ProductVariants.Any(v => v.SizeId == sizeId.Value && v.IsActive));
            }

            // Sắp xếp
            switch (sortBy.ToLower())
            {
                case "lowestprice":
                    productsQuery = productsQuery.OrderBy(p => p.DiscountedPrice ?? p.Price);
                    break;
                case "highestprice":
                    productsQuery = productsQuery.OrderByDescending(p => p.DiscountedPrice ?? p.Price);
                    break;
                case "bestrating":
                    productsQuery = productsQuery.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0);
                    break;
                case "bestselling":
                    productsQuery = productsQuery.OrderByDescending(p => p.OrderDetails.Count);
                    break;
                case "newest":
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Tính toán phân trang
            var totalRecords = productsQuery.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var products = productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu qua ViewBag
            ViewBag.Categories = categories;
            ViewBag.Subcategories = subcategories;
            ViewBag.Colors = colors;
            ViewBag.Sizes = sizes;
            ViewBag.RelatedCategories = relatedCategories; // Thêm danh sách 3 danh mục đầu tiên
            ViewBag.CategoryProductCounts = categoryProductCounts;
            ViewBag.SubcategoryProductCounts = subcategoryProductCounts;
            ViewBag.ColorVariantCounts = colorVariantCounts;
            ViewBag.SizeVariantCounts = sizeVariantCounts;
            ViewBag.RatingCounts = ratingCounts;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSubcategoryId = subcategoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.ColorId = colorId;
            ViewBag.SizeId = sizeId;
            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            return View("hangmoive", products);
        }

        // GET: HangMoiVe/GetProductDetail
        [HttpGet]
        public ActionResult GetProductDetail(int id)
        {
            var product = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductVariants.Select(v => v.Size))
                .Include(p => p.ProductVariants.Select(v => v.Color))
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .SingleOrDefault(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." }, JsonRequestBehavior.AllowGet);
            }

            var productData = new
            {
                id = product.Id,
                name = product.Name,
                categoryName = product.Subcategory.Category.Name,
                subcategoryName = product.Subcategory.Name,
                imageUrl = product.ImageUrl,
                price = product.Price,
                discountedPrice = product.DiscountedPrice,
                description = product.Description,
                averageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                reviewCount = product.Reviews.Count,
                productImages = product.ProductImages.Select(pi => new
                {
                    id = pi.Id,
                    imageUrl = pi.ImageUrl,
                    isMain = pi.IsMain
                }).ToList(),
                productVariants = product.ProductVariants.Where(v => v.IsActive && v.StockQuantity > 0).Select(v => new
                {
                    id = v.Id,
                    sizeName = v.Size.Name,
                    colorName = v.Color.Name,
                    stockQuantity = v.StockQuantity,
                    variantPrice = v.VariantPrice ?? product.Price,
                    variantDiscountedPrice = v.VariantDiscountedPrice,
                    variantImageUrl = v.VariantImageUrl
                }).ToList()
            };

            return Json(new { success = true, data = productData }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult sanphamdetailt(int id)
        {
            var product = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.Subcategory.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductVariants.Select(v => v.Size))
                .Include(p => p.ProductVariants.Select(v => v.Color))
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .SingleOrDefault(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return HttpNotFound("Sản phẩm không tồn tại.");
            }

            // Lấy 4 sản phẩm gợi ý ngẫu nhiên từ cùng danh mục con
            var relatedProducts = _context.Products
                .Include(p => p.Subcategory)
                .Include(p => p.Reviews)
                .Where(p => p.SubcategoryId == product.SubcategoryId && p.Id != product.Id && p.IsActive)
                .OrderBy(r => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View("sanphamdetailt", product);
        }
    }
}