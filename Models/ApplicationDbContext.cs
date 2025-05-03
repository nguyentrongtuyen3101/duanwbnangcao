using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace doanwebnangcao.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("name=SocialNetworkConnection")
        {
            Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Address> Addresses { get; set; }
        
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Size> Sizes { get; set; } // Thêm DbSet cho Size
        public DbSet<Color> Colors { get; set; } // Thêm DbSet cho Color
        public DbSet<ProductVariant> ProductVariants { get; set; } // Thêm DbSet cho ProductVariant
        public DbSet<ChatMessage> ChatMessages { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Quan hệ tin nhắn (Messages)
            modelBuilder.Entity<ChatMessage>()
                .HasRequired(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ChatMessage>()
                .HasRequired(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .WillCascadeOnDelete(false);
        }
    }
}