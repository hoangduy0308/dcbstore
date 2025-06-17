using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        
        // --- THAY ĐỔI QUAN TRỌNG ---
        // Thay thế ProductVariantId bằng ProductId
        public int ProductId { get; set; }
        
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; } // Giá tại thời điểm mua

        public Order? Order { get; set; }
        
        // Trỏ đến sản phẩm chung, không còn biến thể
        public Product? Product { get; set; }
    }
}