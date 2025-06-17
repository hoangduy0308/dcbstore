using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    // Đây là model CartItem được ánh xạ vào database
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với Cart (giỏ hàng của người dùng)
        public int CartId { get; set; }
        [ForeignKey("CartId")]
        public Cart Cart { get; set; } = null!; // Navigation property

        // Khóa ngoại liên kết với Product (sản phẩm)
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!; // Navigation property

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        // Thuộc tính tính toán để lấy tổng giá của mặt hàng này (không lưu vào DB)
        [NotMapped] 
        public decimal Total => Quantity * Product.Price;
    }
}