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
        
        // Đã xóa ProductVariants và thêm lại ProductImages
        public DbSet<ProductImage> ProductImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Chỉ định kiểu dữ liệu cho các cột decimal để tránh mất dữ liệu
            modelBuilder.Entity<Order>().Property(o => o.Total).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<OrderDetail>().Property(od => od.Price).HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18, 2)"); // Thêm cho Product

            // --- DỮ LIỆU MẪU ĐÃ ĐƯỢC CẬP NHẬT ---
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Thiết bị điện tử" },
                new Category { Id = 2, Name = "Sách" },
                new Category { Id = 3, Name = "Mỹ phẩm & Nước hoa" }
            );

            // Cập nhật Product: Thêm Price và Stock trực tiếp
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop ThinkPad T14", Description = "Laptop doanh nhân mạnh mẽ, bền bỉ.", CategoryId = 1, Price = 25000000m, Stock = 10 },
                new Product { Id = 2, Name = "Tiểu thuyết 'Nhà Giả Kim'", Description = "Cuốn sách bán chạy nhất mọi thời đại của Paulo Coelho.", CategoryId = 2, Price = 69000m, Stock = 100 },
                new Product { Id = 3, Name = "Nước hoa Chanel No. 5", Description = "Hương thơm cổ điển và quyến rũ cho phái nữ.", CategoryId = 3, Price = 3500000m, Stock = 20 },
                new Product { Id = 4, Name = "Tai nghe Sony WH-1000XM5", Description = "Tai nghe chống ồn chủ động hàng đầu thế giới.", CategoryId = 1, Price = 8500000m, Stock = 30 }
            );

            // Thêm dữ liệu mẫu cho ProductImage
            modelBuilder.Entity<ProductImage>().HasData(
                new ProductImage { Id = 1, ProductId = 1, Url = "/images/products/laptop-thinkpad.jpg" },
                new ProductImage { Id = 2, ProductId = 2, Url = "/images/products/nha-gia-kim.jpg" },
                new ProductImage { Id = 3, ProductId = 3, Url = "/images/products/chanel-no5.jpg" },
                new ProductImage { Id = 4, ProductId = 4, Url = "/images/products/sony-headphone.jpg" }
            );
        }
    }
}