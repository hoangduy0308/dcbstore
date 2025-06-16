using Microsoft.AspNetCore.Mvc;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using System.Linq;

namespace DCBStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem == null)
            {
                // Thêm mới, bao gồm cả ImageUrl
                cart.Add(new CartItem 
                { 
                    ProductId = productId, 
                    ProductName = product.Name, 
                    Price = product.Price, 
                    Quantity = 1,
                    ImageUrl = product.ImageUrl // Thêm ImageUrl vào giỏ hàng
                });
            }
            else
            {
                cartItem.Quantity++;
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            var cartItemCount = cart.Sum(item => item.Quantity);
            return Ok(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", newCartCount = cartItemCount });
        }

        // --- BẮT ĐẦU VÙNG CODE MỚI ---

        // Action để xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            // Trả về tổng số tiền và số lượng mới để cập nhật giao diện
            return Json(new { 
                success = true, 
                newTotal = cart.Sum(i => i.Total), 
                newCartCount = cart.Sum(i => i.Quantity) 
            });
        }

        // Action để tăng số lượng
        [HttpPost]
        public IActionResult IncreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                itemToUpdate.Quantity++;
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return Json(new { 
                success = true, 
                newQuantity = itemToUpdate?.Quantity, 
                newItemTotal = itemToUpdate?.Total, 
                newTotal = cart.Sum(i => i.Total),
                newCartCount = cart.Sum(i => i.Quantity) 
            });
        }

        // Action để giảm số lượng
        [HttpPost]
        public IActionResult DecreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (itemToUpdate.Quantity > 1)
                {
                    itemToUpdate.Quantity--;
                }
                else
                {
                    // Nếu số lượng là 1, xóa luôn sản phẩm
                    cart.Remove(itemToUpdate);
                }
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return Json(new { 
                success = true, 
                newQuantity = itemToUpdate?.Quantity, 
                newItemTotal = itemToUpdate?.Total, 
                newTotal = cart.Sum(i => i.Total),
                newCartCount = cart.Sum(i => i.Quantity),
                itemRemoved = itemToUpdate == null || itemToUpdate.Quantity < 1 // Báo cho client biết item đã bị xóa
            });
        }

        // --- KẾT THÚC VÙNG CODE MỚI ---
    }
}
