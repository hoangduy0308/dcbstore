using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        // ← only this one constructor!
        public OrdersController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = status;

            if (oldStatus != status)
            {
                // Completed → increase SoldQuantity
                if (status == OrderStatus.Completed && oldStatus != OrderStatus.Completed)
                {
                    foreach (var d in order.OrderDetails)
                    {
                        d.Product.SoldQuantity += d.Quantity;
                        _context.Products.Update(d.Product);
                    }
                }
                // Cancelled → restock and roll back sold
                else if (status == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
                {
                    foreach (var d in order.OrderDetails)
                    {
                        d.Product.Stock += d.Quantity;
                        _context.Products.Update(d.Product);
                    }
                    if (oldStatus == OrderStatus.Completed)
                    {
                        foreach (var d in order.OrderDetails)
                        {
                            d.Product.SoldQuantity -= d.Quantity;
                            if (d.Product.SoldQuantity < 0)
                                d.Product.SoldQuantity = 0;
                            _context.Products.Update(d.Product);
                        }
                    }
                }
            }

            _context.Update(order);
            await _context.SaveChangesAsync();

            // Send notification email
            var subject = $"Đơn hàng #{order.Id} đã chuyển trạng thái";
            var html = $@"
                <p>Xin chào {order.User.FullName},</p>
                <p>Đơn hàng của bạn (ID: {order.Id}) hiện là: <strong>{status}</strong>.</p>
                <p>Cảm ơn bạn đã mua sắm!</p>";

            await _emailSender.SendEmailAsync(order.User.Email, subject, html);

            TempData["StatusMessage"] = "Cập nhật trạng thái và gửi email thành công.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}
