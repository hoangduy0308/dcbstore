using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DCBStore.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // === BẮT ĐẦU THÊM MỚI ===
        [Display(Name = "Đã xóa")]
        public bool IsDeleted { get; set; } = false; // Mặc định là chưa xóa
        // === KẾT THÚC THÊM MỚI ===

        // Một Category có thể có nhiều Product
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}