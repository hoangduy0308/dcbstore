using DCBStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: /Products
        // Đã được cập nhật để hỗ trợ Tìm kiếm và Phân trang
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô tìm kiếm
            ViewData["CurrentFilter"] = searchString;

            // Bắt đầu với một câu truy vấn cơ bản
            var productsQuery = from p in _context.Products
                                select p;

            // Nếu có từ khóa tìm kiếm, lọc kết quả
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(s => s.Name.Contains(searchString));
            }

            int pageSize = 8; // Số lượng sản phẩm mỗi trang
            int currentPage = pageNumber ?? 1; // Số trang hiện tại, mặc định là 1

            // Lấy tổng số sản phẩm sau khi lọc
            var count = await productsQuery.CountAsync();
            // Lấy danh sách sản phẩm cho trang hiện tại
            var products = await productsQuery.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            // Gửi thông tin phân trang đến View
            ViewData["TotalPages"] = (int)Math.Ceiling(count / (double)pageSize);
            ViewData["CurrentPage"] = currentPage;

            return View(products);
        }


        // GET: /Products/Details/{id}
        // Đã được cập nhật để sử dụng async và kiểm tra id null
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tìm sản phẩm trong CSDL có Id tương ứng một cách bất đồng bộ
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            // Nếu không tìm thấy sản phẩm, trả về lỗi 404 Not Found
            if (product == null)
            {
                return NotFound();
            }

            // Nếu tìm thấy, truyền đối tượng product cho View để hiển thị
            return View(product);
        }
    }
}
