using Microsoft.AspNetCore.Mvc;
using DCBStore.Data;
using DCBStore.Models; 
using DCBStore.Helpers; // Để sử dụng SessionHelper
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Để quản lý người dùng
using Microsoft.AspNetCore.Authorization; // Vẫn cần cho các action khác
using System; 

namespace DCBStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 
        private const string SessionCartKey = "CartSession"; 

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager; 
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) // Người dùng chưa đăng nhập, sử dụng giỏ hàng Session
            {
                var sessionCartItems = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(SessionCartKey) ?? new List<SessionCartItem>();
                return View(sessionCartItems); 
            }
            else // Người dùng đã đăng nhập, lấy giỏ hàng từ Database
            {
                var userCart = await _context.Carts
                                             .Include(c => c.CartItems) 
                                                 .ThenInclude(ci => ci.Product) 
                                                     .ThenInclude(p => p.Images) 
                                             .FirstOrDefaultAsync(c => c.UserId == userId);

                if (userCart == null)
                {
                    userCart = new Models.Cart { UserId = userId, CreatedDate = DateTime.Now, LastModifiedDate = DateTime.Now };
                    _context.Carts.Add(userCart);
                    await _context.SaveChangesAsync();
                }

                var sessionCartItemsOnLogin = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(SessionCartKey);
                if (sessionCartItemsOnLogin != null && sessionCartItemsOnLogin.Any())
                {
                    foreach (var sessionItem in sessionCartItemsOnLogin)
                    {
                        var product = await _context.Products.FindAsync(sessionItem.ProductId);
                        if (product != null && product.Stock >= sessionItem.Quantity)
                        {
                            var dbCartItem = userCart.CartItems.FirstOrDefault(ci => ci.ProductId == sessionItem.ProductId);
                            if (dbCartItem != null)
                            {
                                if (dbCartItem.Quantity + sessionItem.Quantity <= product.Stock)
                                {
                                    dbCartItem.Quantity += sessionItem.Quantity;
                                }
                            }
                            else
                            {
                                userCart.CartItems.Add(new Models.CartItem
                                {
                                    ProductId = sessionItem.ProductId,
                                    Quantity = sessionItem.Quantity,
                                    Product = product 
                                });
                            }
                        }
                    }
                    userCart.LastModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.Remove(SessionCartKey); 
                }
                
                List<SessionCartItem> cartItemsForView = new List<SessionCartItem>();
                foreach (var dbItem in userCart.CartItems)
                {
                    cartItemsForView.Add(new SessionCartItem
                    {
                        ProductId = dbItem.ProductId,
                        ProductName = dbItem.Product.Name, 
                        Price = dbItem.Product.Price,     
                        Quantity = dbItem.Quantity,
                        ImageUrl = dbItem.Product.Images?.FirstOrDefault()?.Url,
                        Product = dbItem.Product 
                    });
                }
                return View(cartItemsForView); 
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        // BẮT ĐẦU SỬA ĐỔI: Bỏ [Authorize] ở đây và kiểm tra thủ công
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated) // Kiểm tra thủ công nếu người dùng chưa đăng nhập
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
            }
            // KẾT THÚC SỬA ĐỔI

            var userId = _userManager.GetUserId(User); // Đảm bảo userId có giá trị nếu đã xác thực

            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }
            
            if (quantity <= 0)
            {
                return Ok(new { success = false, message = "Số lượng sản phẩm phải lớn hơn 0." });
            }

            var userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (userCart == null)
            {
                userCart = new Models.Cart { UserId = userId, CreatedDate = DateTime.Now, LastModifiedDate = DateTime.Now };
                _context.Carts.Add(userCart);
                await _context.SaveChangesAsync(); 
            }

            var dbCartItem = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);

            if (dbCartItem != null)
            {
                if (dbCartItem.Quantity + quantity <= product.Stock)
                {
                    dbCartItem.Quantity += quantity;
                }
                else
                {
                    return Ok(new { success = false, message = $"Chỉ có thể thêm tối đa {product.Stock - dbCartItem.Quantity} sản phẩm này vào giỏ. Tổng số lượng sẽ vượt quá tồn kho." });
                }
            }
            else 
            {
                if (product.Stock >= quantity)
                {
                    userCart.CartItems.Add(new Models.CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        Product = product 
                    });
                }
                else
                {
                    return Ok(new { success = false, message = "Sản phẩm không đủ số lượng trong kho." }); 
                }
            }
            userCart.LastModifiedDate = DateTime.Now; 
            await _context.SaveChangesAsync(); 

            var cartItemCount = userCart.CartItems.Sum(item => item.Quantity);
            return Ok(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", newCartCount = cartItemCount });
        }
        
        [HttpPost]
        [Authorize] // Giữ [Authorize] cho các hành động này
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = _userManager.GetUserId(User);
            var userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToRemove = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                userCart.CartItems.Remove(itemToRemove);
                userCart.LastModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, newTotal = userCart.CartItems.Sum(i => i.Total), newCartCount = userCart.CartItems.Sum(i => i.Quantity) });
        }

        [HttpPost]
        [Authorize] // Giữ [Authorize] cho các hành động này
        public async Task<IActionResult> IncreaseQuantity(int productId)
        {
            var userId = _userManager.GetUserId(User);
            var userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToUpdate = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            
            if (itemToUpdate != null)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null && itemToUpdate.Quantity < product.Stock)
                {
                    itemToUpdate.Quantity++;
                    userCart.LastModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = userCart.CartItems.Sum(i => i.Total), newCartCount = userCart.CartItems.Sum(i => i.Quantity) });
        }

        [HttpPost]
        [Authorize] // Giữ [Authorize] cho các hành động này
        public async Task<IActionResult> DecreaseQuantity(int productId)
        {
            var userId = _userManager.GetUserId(User);
            var userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToUpdate = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (itemToUpdate.Quantity > 1) itemToUpdate.Quantity--; // Giảm số lượng nếu > 1
                else userCart.CartItems.Remove(itemToUpdate); // Xóa khỏi DB nếu số lượng là 1
                userCart.LastModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = userCart.CartItems.Sum(i => i.Total), newCartCount = userCart.CartItems.Sum(i => i.Quantity), itemRemoved = itemToUpdate == null || itemToUpdate.Quantity < 1 });
        }
    }
}