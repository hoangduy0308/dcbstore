using System.ComponentModel.DataAnnotations;

namespace DCBStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public DateTime OrderDate { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string? CustomerName { get; set; } // Thêm trường tên khách hàng

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        public string? ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public string? Notes { get; set; } // Thêm trường ghi chú

        public decimal TotalAmount { get; set; }
        
        [Required]
        public string? PaymentMethod { get; set; } // <-- THÊM DÒNG NÀY

        public string? Status { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}