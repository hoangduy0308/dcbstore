// File: Models/Product.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")] 
        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không thể là số âm")]
        public int Stock { get; set; } // Giữ lại thuộc tính Stock

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // Thêm dòng này để tạo mối quan hệ một-nhiều với ProductImage
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}