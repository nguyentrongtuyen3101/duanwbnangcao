using Microsoft.Owin.BuilderProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [Required]
        public int ShippingAddressId { get; set; }

        [Required]
        public int ShippingMethodId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal ShippingCost { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        public int? CouponId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountApplied { get; set; } = 0;

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Address ShippingAddress { get; set; }
        public virtual ShippingMethod ShippingMethod { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }
        public virtual Coupon Coupon { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}