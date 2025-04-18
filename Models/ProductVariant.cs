using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int SizeId { get; set; }

        [Required]
        public int ColorId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? VariantPrice { get; set; } // Giá riêng của biến thể (nếu khác giá sản phẩm)

        [Range(0, double.MaxValue)]
        public decimal? VariantDiscountedPrice { get; set; } // Giá khuyến mãi của biến thể

        [StringLength(500)]
        public string VariantImageUrl { get; set; } // Ảnh riêng của biến thể (nếu có)

        public bool IsActive { get; set; } = true;

        public virtual Product Product { get; set; }
        public virtual Size Size { get; set; }
        public virtual Color Color { get; set; }
        public virtual ICollection<CartDetail> CartDetails { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}