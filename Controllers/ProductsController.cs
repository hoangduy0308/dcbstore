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

            var productsQuery = _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.Images) 
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
            var count = await productsQuery.CountAsync();
            var products = await productsQuery
                                     .Skip(((pageNumber ?? 1) - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling(count / (double)pageSize);
            ViewData["CurrentPage"] = pageNumber ?? 1;

            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var product = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.Images)
                                         // BẮT ĐẦU THÊM MỚI: Bao gồm các đánh giá và thông tin người dùng đánh giá
                                         .Include(p => p.Reviews.OrderByDescending(r => r.ReviewDate))
                                            .ThenInclude(r => r.User)
                                         // KẾT THÚC THÊM MỚI
                                         .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}