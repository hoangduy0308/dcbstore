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

        public async Task<IActionResult> Index()
        {
            // Lấy 4 sản phẩm nổi bật nhất (chưa bị xóa và bán chạy nhất)
            var featuredProducts = await _context.Products
                .Where(p => !p.IsDeleted) // Chỉ lấy sản phẩm chưa bị xóa
                .OrderByDescending(p => p.SoldQuantity) // Sắp xếp theo bán chạy
                .Include(p => p.Images) // Lấy kèm hình ảnh
                .Take(4) // SỬA ĐỔI: Chỉ lấy 4 sản phẩm đầu tiên
                .ToListAsync();
                
            return View(featuredProducts);
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