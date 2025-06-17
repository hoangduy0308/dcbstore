using DCBStore.Data; // Thêm dòng này để nhận diện ApplicationUser
// Thêm dòng này để nhận diện Product, nếu Product ở namespace khác
// Ví dụ: using DCBStore.Models; (nếu Product nằm trong cùng namespace)

namespace DCBStore.Models
{
    public class SessionCartItem
    {
        // Thay thế VariantId bằng ProductId
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }

        public decimal Total => Price * Quantity;

        // BẮT ĐẦU PHẦN THÊM MỚI
        // Thuộc tính để lưu đối tượng Product liên quan
        public Product Product { get; set; } = null!; 

        // Thuộc tính để lưu User ID liên quan đến CartItem này (nếu cần cho lưu trữ DB hoặc logic BuyNow)
        public string UserId { get; set; } = string.Empty; 
        // KẾT THÚC PHẦN THÊM MỚI
    }
}