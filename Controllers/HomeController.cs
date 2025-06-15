using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DCBStore.Models; // Quan trọng: Thêm dòng này

namespace DCBStore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Action này sẽ được chạy khi người dùng truy cập vào trang chủ
    public IActionResult Index()
    {
        // Tạo một danh sách sản phẩm đa dạng
        var products = new List<Product>
        {
            new Product 
            { 
                Id = 1, 
                Name = "Laptop ThinkPad T14", 
                Description = "Laptop doanh nhân mạnh mẽ, bền bỉ.", 
                Price = 25000000m, 
                ImageUrl = "/images/laptop-thinkpad.jpg" 
            },
            new Product 
            { 
                Id = 2, 
                Name = "Tiểu thuyết 'Nhà Giả Kim'", 
                Description = "Cuốn sách bán chạy nhất mọi thời đại của Paulo Coelho.", 
                Price = 69000m, 
                ImageUrl = "/images/nha-gia-kim.jpg" 
            },
            new Product 
            { 
                Id = 3, 
                Name = "Nước hoa Chanel No. 5", 
                Description = "Hương thơm cổ điển và quyến rũ cho phái nữ.", 
                Price = 3500000m, 
                ImageUrl = "/images/chanel-no5.jpg" 
            },
            new Product
            {
                Id = 4,
                Name = "Tai nghe Sony WH-1000XM5",
                Description = "Tai nghe chống ồn chủ động hàng đầu thế giới.",
                Price = 8500000m,
                ImageUrl = "/images/sony-headphone.jpg"
            }
        };

        // Truyền danh sách sản phẩm này đến View
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