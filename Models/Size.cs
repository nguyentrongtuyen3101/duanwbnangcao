using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class Size
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Ví dụ: S, M, L, XL

        [StringLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ProductVariant> ProductVariants { get; set; }
    }
}