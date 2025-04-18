using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class CartDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductVariantId { get; set; } // Thay ProductId bằng ProductVariantId

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; } // Giá của biến thể tại thời điểm thêm vào giỏ hàng

        // Navigation properties
        public virtual Cart Cart { get; set; }
        public virtual ProductVariant ProductVariant { get; set; }
    }
}