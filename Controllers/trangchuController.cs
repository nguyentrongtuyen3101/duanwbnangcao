using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;
using doanwebnangcao.ViewModels;
using System.Data.Entity;

namespace doanwebnangcao.Controllers
{
    public class trangchuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public trangchuController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: trangchu
        public ActionResult trangchu()
        {
            var viewModel = new HomeViewModel
            {
                // TOP TRENDING: Lấy 12 sản phẩm mới nhất cho mỗi danh mục
                TopTrending = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.Subcategory.CategoryId)
                    .SelectMany(g => g.OrderByDescending(p => p.CreatedAt).Take(12))
                    .ToList(),

                // DEAL OF THE DAY: Sản phẩm có giảm giá
                DealOfTheDay = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && p.DiscountedPrice.HasValue)
                    .OrderByDescending(p => p.DiscountedPrice)
                    .Take(2)
                    .ToList(),

                // NEW ARRIVALS: Sản phẩm mới nhất
                NewArrivals = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .ToList(),

                // FEATURED PRODUCTS: Sản phẩm có đánh giá cao
                FeaturedProducts = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && p.Reviews.Any())
                    .OrderByDescending(p => p.Reviews.Average(r => r.Rating))
                    .Take(4)
                    .ToList(),

                // SPECIAL PRODUCTS: Sản phẩm bán chạy
                SpecialProducts = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.OrderDetails.Count)
                    .Take(3)
                    .ToList(),

                // WEEKLY PRODUCTS: Sản phẩm có giảm giá, chọn ngẫu nhiên
                WeeklyProducts = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews) // Thêm Reviews để đồng bộ
                    .Where(p => p.IsActive && p.DiscountedPrice.HasValue)
                    .OrderBy(x => Guid.NewGuid()) // Chọn ngẫu nhiên
                    .Take(3)
                    .ToList(),

                // FLASH PRODUCTS: Sản phẩm mới nhất
                FlashProducts = _context.Products
                    .Include(p => p.Subcategory)
                    .Include(p => p.Subcategory.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews) // Thêm Reviews để đồng bộ
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToList(),

                // Lấy danh sách Categories
                Categories = _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}