using Microsoft.Owin.BuilderProperties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace doanwebnangcao.Models
{
    public enum Role
    {
        Admin,
        User
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(256)]
        public string Password { get; set; } // Có thể null nếu đăng ký qua Google/Facebook

        [Required]
        public Role Role { get; set; }

        public string ExternalId { get; set; } // Lưu ID từ Google/Facebook
        public string Provider { get; set; } // Lưu loại provider: "Google" hoặc "Facebook"

        // Trường mới
        [StringLength(15)]
        public string PhoneNumber { get; set; } // Không có [Required], nên có thể null

        public bool IsActive { get; set; } = true; // Giá trị mặc định là true, không cần nhập khi đăng ký

        // Navigation properties
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Address> Addresses { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}