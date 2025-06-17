using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Các thuộc tính của biến thể.
        // Tạm thời chúng ta sẽ định nghĩa sẵn một vài thuộc tính phổ biến.
        // Bạn có thể để trống (null) nếu sản phẩm không có thuộc tính đó.
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? Storage { get; set; } // Ví dụ: 256GB, 512GB

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        public int Stock { get; set; }

        public string? ImageUrl { get; set; } // Mỗi biến thể có thể có ảnh riêng
    }
}