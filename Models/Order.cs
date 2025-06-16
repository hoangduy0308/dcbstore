using System.ComponentModel.DataAnnotations;

namespace DCBStore.Models
{
    public enum OrderStatus
    {
        Pending,
        Shipped,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public DateTime OrderDate { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        public string? ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public string? Notes { get; set; }

        public decimal Total { get; set; }
        
        [Required]
        public string? PaymentMethod { get; set; }

        public OrderStatus Status { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}