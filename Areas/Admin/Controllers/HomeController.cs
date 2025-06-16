using DCBStore.Areas.Admin.Models;
using DCBStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Bổ sung DbContext và UserManager để truy vấn dữ liệu
        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Home/Index hoặc /Admin
        public async Task<IActionResult> Index()
        {
            // Lấy ngày đầu tiên và cuối cùng của tháng hiện tại
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Tính toán các số liệu
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate <= lastDayOfMonth)
                .SumAsync(o => o.TotalAmount);

            var newOrdersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == today);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Processing" || o.Status == "Pending");
                
            var totalUsers = await _context.Users.CountAsync();


            // Tạo ViewModel và gán dữ liệu
            var viewModel = new DashboardViewModel
            {
                MonthlyRevenue = monthlyRevenue,
                NewOrdersToday = newOrdersToday,
                PendingOrders = pendingOrders,
                NewUsersThisMonth = totalUsers // Tạm thời lấy tổng số người dùng
            };

            // Gửi ViewModel đến View
            return View(viewModel);
        }
    }
}
