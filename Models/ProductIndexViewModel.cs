using System.Collections.Generic;

namespace DCBStore.Models
{
    // Lớp này sẽ chứa mọi thứ mà View cần
    public class ProductIndexViewModel
    {
        // Danh sách sản phẩm để hiển thị
        public List<Product> Products { get; set; } = new List<Product>();

        // Thông tin về các bộ lọc hiện tại
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public string? Status { get; set; }
        public string? SortOrder { get; set; }
        
        // Danh sách danh mục để hiển thị trong bộ lọc
        public List<Category> Categories { get; set; } = new List<Category>();

        // Thông tin phân trang
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}