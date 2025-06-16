using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DCBStore.Models;
using DCBStore.Data; // Thêm dòng này để sử dụng DbContext
using Microsoft.EntityFrameworkCore; // Thêm dòng này để sử dụng ToListAsync
using System.Linq; // Thêm dòng này để sử dụng Where, Skip, Take
using System.Threading.Tasks; // Thêm dòng này để sử dụng Task

namespace DCBStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context; // Thay thế ILogger bằng DbContext
        private readonly ILogger<HomeController> _logger;


        // Cập nhật constructor để nhận ApplicationDbContext
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Cập nhật action Index để hỗ trợ tìm kiếm và phân trang
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var productsQuery = from p in _context.Products
                                select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.Contains(searchString));
            }

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
