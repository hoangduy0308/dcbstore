using DCBStore.Data; // <-- Dòng này cần thiết để tham chiếu ApplicationUser
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại đến người dùng
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        // Khóa ngoại đến sản phẩm
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}