using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DCBStore.Models;
using DCBStore.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using DCBStore.Models.ViewModels; // Thêm namespace của ViewModel

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
            // Lấy 8 sản phẩm bán chạy nhất để làm "Sản phẩm nổi bật"
            var featuredProducts = await _context.Products
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.SoldQuantity)
                .Include(p => p.Images)
                .Take(8) 
                .ToListAsync();

            // Lấy 8 sản phẩm mới nhất để làm "Hàng mới về"
            var newArrivals = await _context.Products
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.Id) // Giả sử Id càng lớn thì càng mới
                .Include(p => p.Images)
                .Take(8)
                .ToListAsync();
            
            // Tạo ViewModel và đưa dữ liệu vào
            var viewModel = new HomeViewModel
            {
                FeaturedProducts = featuredProducts,
                NewArrivals = newArrivals
            };
            
            return View(viewModel); // Trả về View với ViewModel
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