using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class Subcategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}