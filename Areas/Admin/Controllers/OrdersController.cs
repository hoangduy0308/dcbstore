using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Thêm để dùng Enum
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                                       .Include(o => o.User) // Câu lệnh này bây giờ sẽ hoạt động
                                       .OrderByDescending(o => o.OrderDate)
                                       .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                                      .Include(o => o.User) // Câu lệnh này bây giờ sẽ hoạt động
                                      .Include(o => o.OrderDetails)
                                          .ThenInclude(od => od.Product)
                                              .ThenInclude(p => p.Images)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status) // THAY ĐỔI: Nhận trực tiếp enum
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status; // Gán trực tiếp enum
            _context.Update(order);
            await _context.SaveChangesAsync();
            
            TempData["StatusMessage"] = "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}