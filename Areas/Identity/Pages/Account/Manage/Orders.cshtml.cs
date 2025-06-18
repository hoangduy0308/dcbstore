#nullable disable

using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Areas.Identity.Pages.Account.Manage
{
    public class OrdersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public OrdersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<Order> Orders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Orders = await _context.Orders
                                   .Where(o => o.UserId == user.Id)
                                   // BẮT ĐẦU THÊM MỚI: Tải chi tiết đơn hàng, sản phẩm và hình ảnh
                                   .Include(o => o.OrderDetails)
                                       .ThenInclude(od => od.Product)
                                           .ThenInclude(p => p.Images)
                                   // KẾT THÚC THÊM MỚI
                                   .OrderByDescending(o => o.OrderDate)
                                   .ToListAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != OrderStatus.Pending)
            {
                return Forbid();
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}