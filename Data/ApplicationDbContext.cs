using DCBStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DCBStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        
        // --- CẬP NHẬT QUAN TRỌNG ---
        // Xóa DbSet ProductImages và thêm DbSet ProductVariants
        public DbSet<ProductVariant> ProductVariants { get; set; }
        // public DbSet<ProductImage> ProductImages { get; set; } // <- ĐÃ XÓA

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // --- SỬA LỖI CẢNH BÁO ---
            // Chỉ định kiểu dữ liệu cho các cột decimal để tránh mất dữ liệu
            modelBuilder.Entity<Order>().Property(o => o.Total).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<OrderDetail>().Property(od => od.Price).HasColumnType("decimal(18, 2)");

            // --- DỮ LIỆU MẪU ĐÃ ĐƯỢC CẬP NHẬT ---
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Thiết bị điện tử" },
                new Category { Id = 2, Name = "Sách" },
                new Category { Id = 3, Name = "Mỹ phẩm & Nước hoa" }
            );

            // Dữ liệu mẫu cho Product (chỉ còn thông tin chung)
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop ThinkPad T14", Description = "Laptop doanh nhân mạnh mẽ, bền bỉ.", CategoryId = 1 },
                new Product { Id = 2, Name = "Tiểu thuyết 'Nhà Giả Kim'", Description = "Cuốn sách bán chạy nhất mọi thời đại của Paulo Coelho.", CategoryId = 2 },
                new Product { Id = 3, Name = "Nước hoa Chanel No. 5", Description = "Hương thơm cổ điển và quyến rũ cho phái nữ.", CategoryId = 3 },
                new Product { Id = 4, Name = "Tai nghe Sony WH-1000XM5", Description = "Tai nghe chống ồn chủ động hàng đầu thế giới.", CategoryId = 1 }
            );

            // Dữ liệu mẫu cho ProductVariant (lưu giá, tồn kho, ảnh)
            modelBuilder.Entity<ProductVariant>().HasData(
                new ProductVariant { Id = 1, ProductId = 1, Price = 25000000m, Stock = 10, ImageUrl = "/images/laptop-thinkpad.jpg" },
                new ProductVariant { Id = 2, ProductId = 2, Price = 69000m, Stock = 100, ImageUrl = "/images/nha-gia-kim.jpg" },
                new ProductVariant { Id = 3, ProductId = 3, Price = 3500000m, Stock = 20, ImageUrl = "/images/chanel-no5.jpg" },
                new ProductVariant { Id = 4, ProductId = 4, Price = 8500000m, Stock = 30, ImageUrl = "/images/sony-headphone.jpg" }
            );
        }
    }
}