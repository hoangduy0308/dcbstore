namespace DCBStore.Models
{
    public class CartItem
    {
        // Thay thế ProductId bằng VariantId
        public int VariantId { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }

        public decimal Total => Price * Quantity;
    }
}