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
        public async Task<IActionResult> AddToCart(int variantId)
        {
            // Tìm biến thể cụ thể, bao gồm cả thông tin sản phẩm gốc
            var variant = await _context.ProductVariants
                                        .Include(v => v.Product)
                                        .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null)
            {
                return NotFound(new { success = false, message = "Phiên bản sản phẩm không tồn tại." });
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(item => item.VariantId == variantId);

            if (cartItem != null) // Nếu biến thể đã có trong giỏ, tăng số lượng
            {
                // Kiểm tra tồn kho trước khi tăng
                if (cartItem.Quantity < variant.Stock)
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
                // Kiểm tra tồn kho
                if (variant.Stock > 0)
                {
                    // Tạo tên sản phẩm đầy đủ
                    string fullName = variant.Product.Name;
                    if (!string.IsNullOrEmpty(variant.Color)) fullName += $" - {variant.Color}";
                    if (!string.IsNullOrEmpty(variant.Size)) fullName += $" - {variant.Size}";
                    if (!string.IsNullOrEmpty(variant.Storage)) fullName += $" - {variant.Storage}";

                    cart.Add(new CartItem 
                    { 
                        VariantId = variantId, 
                        ProductName = fullName, 
                        Price = variant.Price, 
                        Quantity = 1,
                        ImageUrl = variant.ImageUrl 
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
        
        // Các hàm Remove, Increase, Decrease sẽ cần được cập nhật để dùng variantId
        [HttpPost]
        public IActionResult RemoveFromCart(int variantId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(item => item.VariantId == variantId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return Json(new { success = true, newTotal = cart.Sum(i => i.Total), newCartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantity(int variantId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.VariantId == variantId);
            
            if (itemToUpdate != null)
            {
                var variant = await _context.ProductVariants.FindAsync(variantId);
                if (variant != null && itemToUpdate.Quantity < variant.Stock)
                {
                    itemToUpdate.Quantity++;
                    HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
                }
            }
            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = cart.Sum(i => i.Total), newCartCount = cart.Sum(i => i.Quantity) });
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int variantId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.VariantId == variantId);
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