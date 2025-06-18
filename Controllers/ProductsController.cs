using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DCBStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? searchString, 
            int? categoryId, 
            string? sortOrder,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating,
            string? status,
            int pageNumber = 1)
        {
            var productsQuery = _context.Products
                                      .Where(p => !p.IsDeleted) // Chỉ hiển thị sản phẩm chưa bị xóa
                                      .Include(p => p.Category)
                                      .Include(p => p.Images)
                                      .Include(p => p.Reviews)
                                      .AsQueryable();

            // 1. Lọc theo chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.Contains(searchString));
            }

            // 2. Lọc theo danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            // 4. Lọc theo đánh giá tối thiểu
            if (minRating.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            // 5. Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "in-stock")
                {
                    productsQuery = productsQuery.Where(p => p.Stock > 0);
                }
                else if (status == "out-of-stock")
                {
                    productsQuery = productsQuery.Where(p => p.Stock == 0);
                }
            }

            // 6. Sắp xếp
            ViewData["CurrentSort"] = sortOrder;
            switch (sortOrder)
            {
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "best_selling":
                    productsQuery = productsQuery.OrderByDescending(p => p.SoldQuantity);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.Id);
                    break;
            }

            // 7. Phân trang
            int pageSize = 8;
            var count = await productsQuery.CountAsync();
            var pagedProducts = await productsQuery
                                      .Skip((pageNumber - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            // 8. Chuẩn bị ViewModel để gửi tới View
            var viewModel = new ProductIndexViewModel
            {
                Products = pagedProducts,
                // === BẮT ĐẦU SỬA LỖI ===
                // Chỉ lấy các danh mục chưa bị xóa để hiển thị trong bộ lọc
                Categories = await _context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(),
                // === KẾT THÚC SỬA LỖI ===
                SearchString = searchString,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                Status = status,
                SortOrder = sortOrder,
                PageNumber = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var product = await _context.Products
                                      .Where(p => !p.IsDeleted && p.Id == id) 
                                      .Include(p => p.Category)
                                      .Include(p => p.Images)
                                      .Include(p => p.Reviews.OrderByDescending(r => r.ReviewDate))
                                        .ThenInclude(r => r.User)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}