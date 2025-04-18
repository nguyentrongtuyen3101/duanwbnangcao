using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; } // Thay ProductId

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; } = false;

        public virtual ProductVariant ProductVariant { get; set; } // Thay Product
    }
}