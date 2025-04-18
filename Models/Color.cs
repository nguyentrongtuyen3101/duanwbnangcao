using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class Color
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Ví dụ: Đỏ, Xanh, Trắng

        [StringLength(7)]
        public string HexCode { get; set; } // Ví dụ: #FF0000

        [StringLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ProductVariant> ProductVariants { get; set; }
    }
}