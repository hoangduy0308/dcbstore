using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DCBStore.Models; 

namespace DCBStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; } 

        public DbSet<Wishlist> Wishlists { get; set; } 
        public DbSet<WishlistItem> WishlistItems { get; set; } 

        // DbSets cho Cart và CartItem (cho Database)
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; } 

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Chỉ định kiểu dữ liệu cho các cột decimal để tránh mất dữ liệu
            builder.Entity<Order>().Property(o => o.Total).HasColumnType("decimal(18, 2)");
            builder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18, 2)");

            // Cấu hình mối quan hệ 1-nhiều giữa Product và ProductImage
            builder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade); 

            // Cấu hình mối quan hệ 1-nhiều giữa Category và Product
            builder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Cấu hình mối quan hệ 1-nhiều giữa Order và OrderDetail
            builder.Entity<Order>()
                .HasMany(o => o.OrderDetails) 
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ 1-nhiều giữa Product và OrderDetail
            builder.Entity<Product>()
                .HasMany(p => p.OrderDetails) 
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); 
            
            // Cấu hình mối quan hệ 1-1 giữa ApplicationUser và Wishlist
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Wishlist)
                .WithOne(w => w.User)
                .HasForeignKey<Wishlist>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade); 

            // Cấu hình mối quan hệ 1-nhiều giữa Wishlist và WishlistItem
            builder.Entity<Wishlist>()
                .HasMany(w => w.WishlistItems)
                .WithOne(wi => wi.Wishlist)
                .HasForeignKey(wi => wi.WishlistId)
                .OnDelete(DeleteBehavior.Cascade); 

            // Cấu hình mối quan hệ 1-nhiều giữa Product và WishlistItem
            builder.Entity<Product>()
                .HasMany(p => p.WishlistItems)
                .WithOne(wi => wi.Product)
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Cấu hình mối quan hệ 1-1 tùy chọn giữa ApplicationUser và Cart
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Cart) 
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .IsRequired(false) // Đặt là false vì UserId trong Cart là nullable
                .OnDelete(DeleteBehavior.Cascade); 

            // Cấu hình mối quan hệ 1-nhiều giữa Cart và CartItem
            builder.Entity<Cart>()
                .HasMany(c => c.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade); 

            // Cấu hình mối quan hệ 1-nhiều giữa Product và CartItem
            builder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}