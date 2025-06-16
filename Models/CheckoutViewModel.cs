namespace DCBStore.Models
{
    // Lớp này dùng để chứa tất cả dữ liệu cần thiết cho trang thanh toán
    public class CheckoutViewModel
    {
        // Chứa thông tin khách hàng sẽ điền vào form
        public Order Order { get; set; }

        // Chứa danh sách các sản phẩm trong giỏ để hiển thị ở cột tóm tắt
        public List<CartItem> CartItems { get; set; }
    }
}
