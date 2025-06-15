using DCBStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DCBStore.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // === BẮT ĐẦU VÙNG CODE THÊM MỚI ===
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        // === KẾT THÚC VÙNG CODE THÊM MỚI ===

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
                entity.HasData(
                    new Product 
                    { 
                        Id = 1, 
                        Name = "Laptop ThinkPad T14", 
                        Description = "Laptop doanh nhân mạnh mẽ, bền bỉ.", 
                        Price = 25000000m, 
                        ImageUrl = "/images/laptop-thinkpad.jpg" 
                    },
                    new Product 
                    { 
                        Id = 2, 
                        Name = "Tiểu thuyết 'Nhà Giả Kim'", 
                        Description = "Cuốn sách bán chạy nhất mọi thời đại của Paulo Coelho.", 
                        Price = 69000m, 
                        ImageUrl = "/images/nha-gia-kim.jpg" 
                    },
                    new Product 
                    { 
                        Id = 3, 
                        Name = "Nước hoa Chanel No. 5", 
                        Description = "Hương thơm cổ điển và quyến rũ cho phái nữ.", 
                        Price = 3500000m, 
                        ImageUrl = "/images/chanel-no5.jpg" 
                    },
                    new Product
                    {
                        Id = 4,
                        Name = "Tai nghe Sony WH-1000XM5",
                        Description = "Tai nghe chống ồn chủ động hàng đầu thế giới.",
                        Price = 8500000m,
                        ImageUrl = "/images/sony-headphone.jpg"
                    }
                );
            });
        }
    }
}