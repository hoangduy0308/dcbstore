using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DCBStore.Models;
using DCBStore.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var productsQuery = from p in _context.Products
                                select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.Contains(searchString));
            }

            // Đảm bảo tải cả hình ảnh
            productsQuery = productsQuery.Include(p => p.Images);

            // BẮT ĐẦU SỬA ĐỔI: Sắp xếp sản phẩm theo SoldQuantity giảm dần để lấy sản phẩm bán chạy nhất
            productsQuery = productsQuery.OrderByDescending(p => p.SoldQuantity);
            // KẾT THÚC SỬA ĐỔI

            int pageSize = 8; // Hiển thị 8 sản phẩm mỗi trang
            int currentPage = pageNumber ?? 1;

            var count = await productsQuery.CountAsync();
            var products = await productsQuery.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["TotalPages"] = (int)Math.Ceiling(count / (double)pageSize);
            ViewData["CurrentPage"] = currentPage;

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}