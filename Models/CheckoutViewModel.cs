using System.Collections.Generic;

namespace DCBStore.Models
{
    public class CheckoutViewModel
    {
        public Order Order { get; set; } = new Order();

        public List<SessionCartItem> CartItems { get; set; } = new List<SessionCartItem>();

        // === BẮT ĐẦU PHẦN THÊM MỚI ===
        public decimal Subtotal { get; set; } // Thêm thuộc tính Tạm tính
        // === KẾT THÚC PHẦN THÊM MỚI ===

        public decimal TotalAmount { get; set; }

        public string? AppliedCouponCode { get; set; }

        public decimal DiscountAmount { get; set; }
    }
}