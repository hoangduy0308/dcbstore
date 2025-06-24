using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; // BẮT ĐẦU THÊM MỚI: Thêm để dùng ICollection
using DCBStore.Models; // BẮT ĐẦU THÊM MỚI: Thêm để nhận diện Cart, Wishlist, Order

namespace DCBStore.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }

        // BẮT ĐẦU THÊM MỚI: Navigation properties cho các mối quan hệ 1-1 và 1-nhiều
        public Cart? Cart { get; set; } // Mối quan hệ 1-1 với Cart (nullable vì Cart có thể được tạo sau)
        public Wishlist? Wishlist { get; set; } // Mối quan hệ 1-1 với Wishlist (nullable vì Wishlist có thể được tạo sau)
        public ICollection<Order> Orders { get; set; } = new List<Order>(); // Mối quan hệ 1-nhiều với Order
                                                                            // KẾT THÚC THÊM MỚI
        public string FullNameOrUserName()
    {
        return !string.IsNullOrWhiteSpace(FullName) ? FullName : (UserName ?? "");
    }
    }
}