using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; 
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Đảm bảo có

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
                                       .Include(o => o.User) 
                                       .OrderByDescending(o => o.OrderDate)
                                       .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                                      .Include(o => o.User) 
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
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status) // Nhận trực tiếp enum OrderStatus
        {
            // BẮT ĐẦU PHẦN SỬA ĐỔI: Tải order cùng với OrderDetails và Product
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                          .ThenInclude(od => od.Product) // Bao gồm Product để thao tác với Stock/SoldQuantity
                                      .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status; // Lấy trạng thái cũ

            // Cập nhật trạng thái mới
            order.Status = status; 

            // BẮT ĐẦU LOGIC CẬP NHẬT SỐ LƯỢNG ĐÃ BÁN VÀ TỒN KHO
            if (oldStatus != order.Status) // Chỉ xử lý nếu trạng thái thay đổi
            {
                // Logic khi đơn hàng chuyển sang trạng thái "Completed"
                if (order.Status == OrderStatus.Completed)
                {
                    // Nếu trạng thái cũ KHÔNG phải là Completed, thì mới tăng SoldQuantity
                    // (tránh tăng nhiều lần nếu đơn hàng đã Completed rồi được cập nhật lại)
                    if (oldStatus != OrderStatus.Completed)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.SoldQuantity += detail.Quantity;
                                _context.Products.Update(product); // Đánh dấu để cập nhật
                            }
                        }
                    }
                }
                // Logic khi đơn hàng chuyển sang trạng thái "Cancelled"
                else if (order.Status == OrderStatus.Cancelled)
                {
                    // Nếu trạng thái cũ KHÔNG phải là Cancelled, thì mới tăng Stock
                    // (Stock đã bị giảm khi đơn hàng được đặt)
                    if (oldStatus != OrderStatus.Cancelled)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.Stock += detail.Quantity; // Tăng lại số lượng tồn kho
                                _context.Products.Update(product); // Đánh dấu để cập nhật
                            }
                        }
                    }

                    // Nếu trạng thái cũ là Completed, thì giảm SoldQuantity
                    // (vì nó đã không còn là "đã bán" nữa)
                    if (oldStatus == OrderStatus.Completed)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            var product = await _context.Products.FindAsync(detail.ProductId);
                            if (product != null)
                            {
                                product.SoldQuantity -= detail.Quantity; // Giảm số lượng đã bán
                                if (product.SoldQuantity < 0) product.SoldQuantity = 0; // Đảm bảo không âm
                                _context.Products.Update(product); // Đánh dấu để cập nhật
                            }
                        }
                    }
                }
            }
            // KẾT THÚC LOGIC CẬP NHẬT SỐ LƯỢNG ĐÃ BÁN VÀ TỒN KHO

            _context.Update(order); // Cập nhật trạng thái đơn hàng
            await _context.SaveChangesAsync(); // Lưu tất cả thay đổi (trạng thái đơn hàng và sản phẩm)
            
            TempData["StatusMessage"] = "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}