using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int MaxUsage { get; set; }

        [Range(0, int.MaxValue)]
        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Order> Orders { get; set; }
    }
}