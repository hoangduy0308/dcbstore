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
        public int Stock { get; set; } 

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng đã bán không thể là số âm")]
        public int SoldQuantity { get; set; } 

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // Thêm dòng này để tạo mối quan hệ một-nhiều với ProductImage
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // BẮT ĐẦU THÊM MỚI: Navigation properties cho các mối quan hệ 1-nhiều
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>(); // Mối quan hệ với OrderDetail
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>(); // Mối quan hệ với WishlistItem
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>(); // Mối quan hệ với CartItem (mới cho DB)
        // KẾT THÚC THÊM MỚI
    }
}