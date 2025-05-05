using System.Collections.Generic;
using doanwebnangcao.Models;

namespace doanwebnangcao.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> TopTrending { get; set; }
        public List<Product> DealOfTheDay { get; set; }
        public List<Product> NewArrivals { get; set; }
        public List<Product> FeaturedProducts { get; set; }
        public List<Product> SpecialProducts { get; set; }
        public List<Product> WeeklyProducts { get; set; }
        public List<Product> FlashProducts { get; set; }
        public List<Subcategory> Subcategories { get; set; }
        public List<Category> Categories { get; set; }
    }
}