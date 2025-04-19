using doanwebnangcao.Models;
using System.Collections.Generic;

namespace doanwebnangcao.DTO
{
    public class VariantDto
    {
        public int Id { get; set; }
        public int SizeId { get; set; }
        public int ColorId { get; set; }
        public int StockQuantity { get; set; }
        public decimal? VariantPrice { get; set; }
        public string VariantImageUrl { get; set; }
        public bool IsActive { get; set; }
        
    }
}
