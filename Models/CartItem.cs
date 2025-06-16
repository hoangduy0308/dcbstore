namespace DCBStore.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ImageUrl { get; set; } // <-- THÊM DÒNG NÀY
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}