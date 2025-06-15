using DCBStore.Data;
using DCBStore.Models; // Thêm dòng này
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action Index đã có từ trước
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        // --- BẮT ĐẦU THÊM CODE MỚI ---

        // GET: /Admin/Products/Create
        // Action này chỉ hiển thị form để tạo sản phẩm
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Products/Create
        // Action này nhận dữ liệu từ form và lưu vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid) // Kiểm tra xem dữ liệu người dùng nhập có hợp lệ không
            {
                _context.Add(product); // Thêm sản phẩm mới vào DbContext
                await _context.SaveChangesAsync(); // Lưu thay đổi vào CSDL
                return RedirectToAction(nameof(Index)); // Chuyển hướng về trang danh sách sản phẩm
            }
            // Nếu dữ liệu không hợp lệ, hiển thị lại form với các lỗi
            return View(product);
        }

        // --- KẾT THÚC THÊM CODE MỚI ---
    }
}