using DCBStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;

            // Bắt đầu câu truy vấn, bao gồm cả thông tin biến thể
            var productsQuery = _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Variants) // <-- CẬP NHẬT QUAN TRỌNG
                                        .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            ViewData["Categories"] = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            int pageSize = 8;
            int currentPage = pageNumber ?? 1;
            var count = await productsQuery.CountAsync();
            var products = await productsQuery
                                    .Skip((currentPage - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling(count / (double)pageSize);
            ViewData["CurrentPage"] = currentPage;

            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            // Cập nhật để lấy sản phẩm cùng với danh sách các biến thể của nó
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Variants) // <-- THAY THẾ .Images bằng .Variants
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null || !product.Variants.Any()) // Không hiển thị sản phẩm nếu nó không có biến thể nào
            {
                return NotFound();
            }

            return View(product);
        }
    }
}