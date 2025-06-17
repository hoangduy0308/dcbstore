using Microsoft.AspNetCore.Mvc;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
        public async Task<IActionResult> AddToCart(int productId) // Thay đổi: từ variantId sang productId
        {
            // Tìm sản phẩm, bao gồm cả hình ảnh để lấy ảnh đại diện
            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(item => item.ProductId == productId); // Thay đổi: tìm theo ProductId

            if (cartItem != null) // Nếu sản phẩm đã có trong giỏ, tăng số lượng
            {
                if (cartItem.Quantity < product.Stock)
                {
                    cartItem.Quantity++;
                }
                else
                {
                    return Ok(new { success = false, message = "Số lượng trong giỏ đã đạt tối đa." });
                }
            }
            else // Nếu chưa có, thêm mới vào giỏ
            {
                if (product.Stock > 0)
                {
                    cart.Add(new CartItem 
                    { 
                        ProductId = productId, 
                        ProductName = product.Name, 
                        Price = product.Price, 
                        Quantity = 1,
                        // Lấy ảnh đầu tiên làm ảnh đại diện, hoặc null nếu không có ảnh
                        ImageUrl = product.Images.FirstOrDefault()?.Url 
                    });
                }
                else
                {
                     return Ok(new { success = false, message = "Sản phẩm đã hết hàng." });
                }
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            var cartItemCount = cart.Sum(item => item.Quantity);
            return Ok(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", newCartCount = cartItemCount });
        }
        
        [HttpPost]
        public IActionResult RemoveFromCart(int productId) // Thay đổi: từ variantId sang productId
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return Json(new { success = true, newTotal = cart.Sum(i => i.Total), newCartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantity(int productId) // Thay đổi: từ variantId sang productId
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            
            if (itemToUpdate != null)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null && itemToUpdate.Quantity < product.Stock)
                {
                    itemToUpdate.Quantity++;
                    HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
                }
            }
            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = cart.Sum(i => i.Total), newCartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int productId) // Thay đổi: từ variantId sang productId
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (itemToUpdate.Quantity > 1) itemToUpdate.Quantity--;
                else cart.Remove(itemToUpdate);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = cart.Sum(i => i.Total), newCartCount = cart.Sum(i => i.Quantity), itemRemoved = itemToUpdate == null || itemToUpdate.Quantity < 1 });
        }
    }
}