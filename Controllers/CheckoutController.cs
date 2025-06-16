using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;

namespace DCBStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string CartSessionKey = "Cart";

        public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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

            var order = checkoutViewModel.Order;
            var user = await _userManager.GetUserAsync(User);

            order.UserId = user.Id;
            order.OrderDate = DateTime.Now;
            order.TotalAmount = cart.Sum(item => item.Total);
            order.Status = "Processing";

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("Confirmation", new { orderId = order.Id });
        }

        public IActionResult Confirmation(int orderId)
        {
            ViewBag.OrderId = orderId; 
            return View();
        }
    }
}
