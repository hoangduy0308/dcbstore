using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DCBStore.Data;

namespace DCBStore.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Đang chờ xử lý")]
        Pending,
        [Display(Name = "Đang xử lý")]
        Processing,
        [Display(Name = "Đã giao hàng")]
        Shipped,
        [Display(Name = "Hoàn thành")]
        Completed,
        [Display(Name = "Đã hủy")]
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public decimal Total { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "COD";

        public OrderStatus Status { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public ApplicationUser? User { get; set; }
    }
}
