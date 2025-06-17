using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCBStore.Data; // Vẫn cần để nhận diện ApplicationUser nếu các model khác cần

namespace DCBStore.Models
{
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        // BẮT ĐẦU THÊM MỚI: Khóa ngoại đến Wishlist (entity cha)
        [Required]
        public int WishlistId { get; set; }
        [ForeignKey("WishlistId")]
        public Wishlist Wishlist { get; set; } = null!; // Navigation property
        // KẾT THÚC THÊM MỚI

        // Khóa ngoại đến sản phẩm
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}