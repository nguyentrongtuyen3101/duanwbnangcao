using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductVariantId { get; set; } // Thay ProductId bằng ProductVariantId

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; } // Giá của biến thể tại thời điểm đặt hàng

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; } // UnitPrice * Quantity

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual ProductVariant ProductVariant { get; set; }
    }
}