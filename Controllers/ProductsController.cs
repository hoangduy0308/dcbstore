using DCBStore.Data;
using Microsoft.AspNetCore.Mvc;

namespace DCBStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action này sẽ xử lý URL: /Products/Details/{id}
        // Ví dụ: /Products/Details/2
        public IActionResult Details(int id)
        {
            // Tìm sản phẩm trong CSDL có Id tương ứng
            var product = _context.Products.FirstOrDefault(p => p.Id == id);

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