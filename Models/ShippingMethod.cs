using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class ShippingMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; }

        [Range(0, int.MaxValue)]
        public int EstimatedDeliveryDays { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Order> Orders { get; set; }
    }
}