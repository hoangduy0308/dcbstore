using System.Collections.Generic;

namespace DCBStore.Models
{
    // Lớp này dùng để chứa tất cả dữ liệu cần thiết cho trang thanh toán
    public class CheckoutViewModel
    {
        // Chứa thông tin khách hàng sẽ điền vào form.
        // Khởi tạo một đối tượng Order mới để tránh lỗi null khi binding dữ liệu.
        public Order Order { get; set; } = new Order();

        // BẮT ĐẦU SỬA ĐỔI: Thay đổi kiểu của CartItems từ List<CartItem> sang List<SessionCartItem>
        // Chứa danh sách các sản phẩm trong giỏ để hiển thị ở cột tóm tắt.
        // Luôn khởi tạo một danh sách rỗng để có thể thêm item vào mà không bị lỗi.
        public List<SessionCartItem> CartItems { get; set; } = new List<SessionCartItem>();
        // KẾT THÚC SỬA ĐỔI

        // Tổng số tiền của các sản phẩm trong giỏ hàng hoặc sản phẩm mua ngay
        public decimal TotalAmount { get; set; } 
    }
}