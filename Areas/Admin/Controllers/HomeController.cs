using DCBStore.Areas.Admin.Models;
using DCBStore.Data;
using DCBStore.Models;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate <= lastDayOfMonth)
                .SumAsync(o => o.Total);

            var newOrdersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == today);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);
                
            var totalUsers = await _context.Users.CountAsync();

            var viewModel = new DashboardViewModel
            {
                MonthlyRevenue = monthlyRevenue,
                NewOrdersToday = newOrdersToday,
                PendingOrders = pendingOrders,
                NewUsersThisMonth = totalUsers
            };

            return View(viewModel);
        }
    }
}