namespace DCBStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; } // Thêm dấu ?
        public string? Description { get; set; } // Thêm dấu ?
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; } // Thêm dấu ?
    }
}