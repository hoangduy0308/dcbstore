using System.ComponentModel.DataAnnotations;

using DCBStore.Data; // Thêm using này để nhận diện ApplicationUser



namespace DCBStore.Models

{

    public enum OrderStatus

    {

        Pending,     // Chờ xử lý

        Processing,  // Đang xử lý

        Shipped,     // Đã giao hàng

        Completed,   // Hoàn thành

        Cancelled    // Đã hủy

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

       

        // ===> THÊM DÒNG NÀY ĐỂ HOÀN TẤT LIÊN KẾT <===

        public ApplicationUser? User { get; set; }

    }

}