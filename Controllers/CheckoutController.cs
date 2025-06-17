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

            if (!ModelState.IsValid)
            {
                return View("Index", checkoutViewModel);
            }
            
            // --- BẮT ĐẦU LOGIC CẬP NHẬT TỒN KHO VÀ ĐƠN HÀNG ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = checkoutViewModel.Order;
                var user = await _userManager.GetUserAsync(User);

                order.UserId = user.Id;
                order.OrderDate = DateTime.Now;
                order.Total = cart.Sum(item => item.Total);
                order.Status = OrderStatus.Pending;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    // Lấy biến thể từ DB để kiểm tra tồn kho lần cuối
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant == null || variant.Stock < item.Quantity)
                    {
                        // Nếu sản phẩm hết hàng hoặc không đủ, hủy giao dịch và báo lỗi
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' không đủ số lượng tồn kho.";
                        return RedirectToAction("Index", "Cart");
                    }
                    
                    // Trừ số lượng tồn kho
                    variant.Stock -= item.Quantity;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductVariantId = item.VariantId, // <-- SỬ DỤNG VARIANT ID
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