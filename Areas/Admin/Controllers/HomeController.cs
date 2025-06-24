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
using System.Collections.Generic;

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
            var totalProducts = await _context.Products.CountAsync();

            var revenueByMonth = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(x => x.Total)
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var ordersByMonth = await _context.Orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.RevenueByMonth = revenueByMonth;
            ViewBag.OrdersByMonth = ordersByMonth;

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
