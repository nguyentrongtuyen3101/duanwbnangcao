using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using doanwebnangcao.Models;

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
            // Truy vấn danh sách Category, bao gồm Subcategories và Products
            var categories = _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    Subcategories = c.Subcategories
                        .Where(s => s.IsActive)
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            Products = s.Products
                                .Where(p => p.IsActive)
                                .OrderBy(p => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                                .Take(4) // Lấy tối đa 4 sản phẩm
                                .Select(p => new { p.Id, p.Name })
                                .ToList()
                        })
                        .ToList(),
                    ImageUrls = c.Subcategories
                        .Where(s => s.IsActive)
                        .SelectMany(s => s.Products)
                        .Where(p => p.IsActive && p.ImageUrl != null)
                        .OrderBy(p => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                        .Take(2) // Lấy tối đa 2 hình ảnh từ tất cả sản phẩm trong Category
                        .Select(p => p.ImageUrl)
                        .ToList()
                })
                .ToList();

            // Truyền dữ liệu vào ViewBag để sử dụng trong _Layout.cshtml
            ViewBag.Categories = categories.Select(c => new Category
            {
                Id = c.Id,
                Name = c.Name,
                Subcategories = c.Subcategories.Select(s => new Subcategory
                {
                    Id = s.Id,
                    Name = s.Name,
                    Products = s.Products.Select(p => new Product
                    {
                        Id = p.Id,
                        Name = p.Name
                    }).ToList()
                }).ToList()
            }).ToList();

            // Truyền danh sách ImageUrls vào ViewBag riêng biệt
            ViewBag.CategoryImages = categories.ToDictionary(
                c => c.Id,
                c => c.ImageUrls
            );

            return View();
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