using System.Collections.Generic;

namespace DCBStore.Models
{
    public class CheckoutViewModel
    {
        public Order Order { get; set; } = new Order();

        public List<SessionCartItem> CartItems { get; set; } = new List<SessionCartItem>();

        public decimal TotalAmount { get; set; } 

        public string? AppliedCouponCode { get; set; } // Thêm dòng này cho mã giảm giá đã áp dụng
        public decimal DiscountAmount { get; set; } // Thêm dòng này cho số tiền giảm giá
    }
}