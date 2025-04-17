using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; } = false;

        // Navigation property
        public virtual Product Product { get; set; }
    }
}