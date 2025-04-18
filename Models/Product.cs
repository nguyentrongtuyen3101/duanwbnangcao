using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace doanwebnangcao.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubcategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; } // Giá mặc định

        [Range(0, double.MaxValue)]
        public decimal? DiscountedPrice { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } // Tổng tồn kho (tính từ ProductVariants)

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(50)]
        [Index(IsUnique = true)]
        public string ProductCode { get; set; }

        public virtual Subcategory Subcategory { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<CartDetail> CartDetails { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } // Quan hệ với biến thể
    }
}