using Microsoft.AspNetCore.Mvc;
using DCBStore.Data;
using DCBStore.Models;
using DCBStore.Helpers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Globalization;
// Thêm các using cần thiết cho việc render view thành chuỗi
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;

namespace DCBStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SessionCartKey = "CartSession";
        private const string SessionAppliedCouponCode = "AppliedCouponCode";
        private const string SessionCouponDiscount = "CouponDiscount";

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var cartItemsForView = new List<SessionCartItem>();

            if (userId != null)
            {
                await MergeSessionCartToDbCart(userId);
                cartItemsForView = await GetDbCartItems(userId);
            }
            else
            {
                // SỬA LỖI 1: Gọi đúng hàm async
                cartItemsForView = await GetSessionCartItemsAsync();
            }

            var subtotal = cartItemsForView.Sum(item => item.Quantity * item.Price);
            var (discount, appliedCouponCode) = await RecalculateDiscount(subtotal, userId);

            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.Total = subtotal - discount;
            ViewBag.AppliedCouponCode = appliedCouponCode;

            return View(cartItemsForView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ." });
            }

            var userId = _userManager.GetUserId(User);
            int newCartCount;

            if (userId != null)
            {
                var userCart = await GetOrCreateDbCart(userId);
                var cartItem = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);

                int quantityInCart = cartItem?.Quantity ?? 0;
                if (product.Stock < quantityInCart + quantity)
                {
                    return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
                }

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    userCart.CartItems.Add(new Models.CartItem { ProductId = productId, Quantity = quantity });
                }
                userCart.LastModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                newCartCount = userCart.CartItems.Sum(ci => ci.Quantity);
            }
            else
            {
                var cart = await GetSessionCartItemsAsync();
                var cartItem = cart.FirstOrDefault(item => item.ProductId == productId);

                int quantityInCart = cartItem?.Quantity ?? 0;
                if (product.Stock < quantityInCart + quantity)
                {
                    return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
                }

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    cart.Add(new SessionCartItem { ProductId = productId, Quantity = quantity, Product = product });
                }
                HttpContext.Session.SetObjectAsJson(SessionCartKey, cart);
                newCartCount = cart.Sum(ci => ci.Quantity);
            }

            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", newCartCount });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if(userId == null) return Unauthorized();

            var userCart = await GetDbCart(userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToRemove = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                _context.CartItems.Remove(itemToRemove);
                await _context.SaveChangesAsync();
            }

            return await GetCartStateAsJson(userId);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if(userId == null) return Unauthorized();

            var userCart = await GetDbCart(userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToUpdate = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate == null) return Json(new { success = false, message = "Sản phẩm không có trong giỏ." });

            var product = await _context.Products.FindAsync(productId);
            if (product != null && itemToUpdate.Quantity < product.Stock)
            {
                itemToUpdate.Quantity++;
                await _context.SaveChangesAsync();
            }
            else
            {
                return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
            }

            return await GetCartStateAsJson(userId);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if(userId == null) return Unauthorized();

            var userCart = await GetDbCart(userId);
            if (userCart == null) return Json(new { success = false, message = "Giỏ hàng không tồn tại." });

            var itemToUpdate = userCart.CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate == null) return Json(new { success = false, message = "Sản phẩm không có trong giỏ." });

            itemToUpdate.Quantity--;
            if (itemToUpdate.Quantity < 1)
            {
                _context.CartItems.Remove(itemToUpdate);
            }

            await _context.SaveChangesAsync();
            return await GetCartStateAsJson(userId);
        }
        
        #region Private Helper Methods

        private async Task<Models.Cart?> GetDbCart(string userId) =>
            await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        private async Task<Models.Cart> GetOrCreateDbCart(string userId)
        {
            var userCart = await GetDbCart(userId);
            if (userCart == null)
            {
                userCart = new Models.Cart { UserId = userId, CreatedDate = DateTime.Now };
                _context.Carts.Add(userCart);
                await _context.SaveChangesAsync();
            }
            return userCart;
        }

        private async Task<List<SessionCartItem>> GetSessionCartItemsAsync()
        {
            var sessionCart = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(SessionCartKey) ?? new List<SessionCartItem>();
            foreach (var item in sessionCart)
            {
                if (item.Product == null)
                {
                    item.Product = await _context.Products.Include(p => p.Images).AsNoTracking().FirstAsync(p => p.Id == item.ProductId);
                }
            }
            return sessionCart;
        }

        private async Task<List<SessionCartItem>> GetDbCartItems(string userId)
        {
            var userCart = await _context.Carts
                                       .Include(c => c.CartItems)
                                       .ThenInclude(ci => ci.Product)
                                       .ThenInclude(p => p.Images)
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart != null)
            {
                return userCart.CartItems.Select(dbItem => new SessionCartItem
                {
                    ProductId = dbItem.ProductId,
                    ProductName = dbItem.Product.Name,
                    Price = dbItem.Product.Price,
                    Quantity = dbItem.Quantity,
                    ImageUrl = dbItem.Product.Images?.FirstOrDefault()?.Url,
                    Product = dbItem.Product
                }).ToList();
            }
            return new List<SessionCartItem>();
        }

        private async Task MergeSessionCartToDbCart(string userId)
        {
            var sessionCart = await GetSessionCartItemsAsync();
            if (sessionCart.Any())
            {
                var userCart = await GetOrCreateDbCart(userId);
                foreach (var sessionItem in sessionCart)
                {
                    var product = sessionItem.Product;
                    if (product == null || product.IsDeleted) continue;
                    
                    var dbItem = userCart.CartItems.FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                    int quantityInCart = dbItem?.Quantity ?? 0;
                    int quantityToAdd = sessionItem.Quantity;

                    if (product.Stock < quantityInCart + quantityToAdd) continue;

                    if (dbItem != null)
                    {
                        dbItem.Quantity += quantityToAdd;
                    }
                    else
                    {
                        userCart.CartItems.Add(new Models.CartItem { ProductId = sessionItem.ProductId, Quantity = quantityToAdd });
                    }
                }
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(SessionCartKey);
            }
        }

        private async Task<(decimal, string?)> RecalculateDiscount(decimal subtotal, string? userId, string? newCouponCode = null)
        {
            string? appliedCouponCode = newCouponCode ?? HttpContext.Session.GetString(SessionAppliedCouponCode);
            if (string.IsNullOrEmpty(appliedCouponCode)) return (0, null);

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == appliedCouponCode);

            if (coupon == null || !coupon.IsActive || subtotal < coupon.MinOrderAmount)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return (0, null);
            }
            
            decimal discountAmount;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = subtotal * (coupon.Value / 100);
            }
            else
            {
                discountAmount = coupon.Value;
            }

            if (coupon.MaxDiscountAmount > 0)
            {
                discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount);
            }

            discountAmount = Math.Min(discountAmount, subtotal);
            HttpContext.Session.SetString(SessionAppliedCouponCode, appliedCouponCode);
            HttpContext.Session.SetString(SessionCouponDiscount, discountAmount.ToString(CultureInfo.InvariantCulture));

            return (discountAmount, appliedCouponCode);
        }

        private async Task<IActionResult> GetCartStateAsJson(string userId)
        {
            var cartItems = await GetDbCartItems(userId);
            var subtotal = cartItems.Sum(i => i.Quantity * i.Price);
            var (discount, appliedCouponCode) = await RecalculateDiscount(subtotal, userId);
            var total = subtotal - discount;
            var newCartCount = cartItems.Sum(i => i.Quantity);

            return Json(new
            {
                success = true,
                cartHtml = await this.RenderViewToStringAsync("_CartItemsPartial", cartItems),
                subtotal = subtotal.ToString("N0"),
                discount = discount.ToString("N0"),
                total = total.ToString("N0"),
                newCartCount = newCartCount
            });
        }

        #endregion
    }

    // SỬA LỖI 2: Thêm lớp helper này vào cuối file để biên dịch được
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewToStringAsync<TModel>(this Controller controller, string viewName, TModel model)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }
            controller.ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                IViewEngine? viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}