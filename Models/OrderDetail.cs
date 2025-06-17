using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        
        // --- THAY ĐỔI QUAN TRỌNG ---
        // Thay thế ProductId bằng ProductVariantId
        public int ProductVariantId { get; set; }
        
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public Order? Order { get; set; }
        
        // Trỏ đến biến thể sản phẩm cụ thể
        public ProductVariant? ProductVariant { get; set; }
    }
}