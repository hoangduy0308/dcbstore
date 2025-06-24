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
            var today = DateTime.UtcNow.Date;
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfLastMonth = firstDayOfCurrentMonth.AddMonths(-1);

            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfCurrentMonth && o.OrderDate < firstDayOfCurrentMonth.AddMonths(1) && o.Status == OrderStatus.Completed)
                .SumAsync(o => o.Total);

            var lastMonthRevenue = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfLastMonth && o.OrderDate < firstDayOfCurrentMonth && o.Status == OrderStatus.Completed)
                .SumAsync(o => o.Total);

            var totalOrders = await _context.Orders.CountAsync();
            var totalOrdersLastMonth = await _context.Orders.CountAsync(o => o.OrderDate < firstDayOfCurrentMonth);

            var totalUsers = await _userManager.Users.CountAsync();
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= firstDayOfCurrentMonth && u.CreatedAt < firstDayOfCurrentMonth.AddMonths(1));
            
            var newUsersLastMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= firstDayOfLastMonth && u.CreatedAt < firstDayOfCurrentMonth);

            var newOrdersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == today);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending);
            
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalUsers = totalUsers;

            ViewBag.RevenueByMonth = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, TotalRevenue = g.Sum(x => x.Total) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            ViewBag.OrdersByMonth = await _context.Orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            decimal CalculatePercentageChange(decimal current, decimal previous)
            {
                if (previous == 0)
                {
                    return current > 0 ? 100.0m : 0;
                }
                return Math.Round(((current - previous) / previous) * 100, 1);
            }

            var viewModel = new DashboardViewModel
            {
                MonthlyRevenue = monthlyRevenue,
                NewOrdersToday = newOrdersToday,
                PendingOrders = pendingOrders,
                NewUsersThisMonth = newUsersThisMonth,
                RevenueChangePercentage = CalculatePercentageChange(monthlyRevenue, lastMonthRevenue),
                OrdersChangePercentage = CalculatePercentageChange(totalOrders, totalOrdersLastMonth),
                UsersChangePercentage = CalculatePercentageChange(newUsersThisMonth, newUsersLastMonth)
            };

            return View(viewModel);
        }
    }
}