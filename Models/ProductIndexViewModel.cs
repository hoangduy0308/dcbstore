using System.Collections.Generic;

namespace DCBStore.Models
{
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();

        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public string? Status { get; set; }
        public string? SortOrder { get; set; }

        public List<Category> Categories { get; set; } = new List<Category>();

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
