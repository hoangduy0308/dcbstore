using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore; // Thêm
namespace DCBStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSessionKey = "Cart";

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống, không thể thanh toán.";
                return RedirectToAction("Index", "Cart");
            }
            
            var checkoutViewModel = new CheckoutViewModel
            {
                Order = new Order(),
                CartItems = cart
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(CheckoutViewModel checkoutViewModel)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            checkoutViewModel.CartItems = cart;

            // Lấy danh sách ProductId từ giỏ hàng để truy vấn một lần
            var productIds = cart.Select(c => c.ProductId).ToList();
            var productsInDb = await _context.Products
                                             .Where(p => productIds.Contains(p.Id))
                                             .ToListAsync();

            // Kiểm tra tồn kho trước khi vào transaction
            foreach (var item in cart)
            {
                var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' không đủ số lượng tồn kho hoặc không còn tồn tại.";
                    return RedirectToAction("Index", "Cart");
                }
            }


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = checkoutViewModel.Order;
                var user = await _userManager.GetUserAsync(User);

                order.UserId = user.Id;
                order.OrderDate = DateTime.Now;
                order.Total = cart.Sum(item => item.Total);
                order.Status = OrderStatus.Pending; // Sửa lại nếu bạn dùng Enum

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu để lấy OrderId

                foreach (var item in cart)
                {
                    // Lấy lại sản phẩm từ danh sách đã truy vấn
                    var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                    
                    // Trừ số lượng tồn kho
                    product.Stock -= item.Quantity;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId, // Sửa thành ProductId
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(CartSessionKey);

                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình xử lý đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        public IActionResult Confirmation(int orderId)
        {
            ViewBag.OrderId = orderId; 
            return View();
        }
    }
}