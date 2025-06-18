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
using System.Globalization; // Dòng này cần có

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
            List<SessionCartItem> cartItemsForView = new List<SessionCartItem>();
            decimal subtotal = 0;
            decimal discount = 0;
            string appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);

            if (userId == null)
            {
                var sessionCartItems = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(SessionCartKey) ?? new List<SessionCartItem>();
                
                foreach (var sessionItem in sessionCartItems)
                {
                    var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == sessionItem.ProductId);
                    if (product != null)
                    {
                        sessionItem.ProductName = product.Name;
                        sessionItem.Price = product.Price;
                        sessionItem.ImageUrl = product.Images?.FirstOrDefault()?.Url;
                        sessionItem.Product = product;
                        subtotal += sessionItem.Quantity * sessionItem.Price;
                        cartItemsForView.Add(sessionItem);
                    }
                }
                HttpContext.Session.SetObjectAsJson(SessionCartKey, cartItemsForView);
            }
            else
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
                
                userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                             .ThenInclude(ci => ci.Product)
                                                 .ThenInclude(p => p.Images)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);

                if(userCart != null)
                {
                    foreach (var dbItem in userCart.CartItems)
                    {
                        subtotal += dbItem.Quantity * dbItem.Product.Price;
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
                }
            }

            if (HttpContext.Session.TryGetValue(SessionCouponDiscount, out byte[] discountBytes))
            {
                discount = decimal.Parse(System.Text.Encoding.UTF8.GetString(discountBytes), CultureInfo.InvariantCulture);
            }

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
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
            }

            var userId = _userManager.GetUserId(User);

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
        [Authorize] 
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
                _context.CartItems.Remove(itemToRemove);
                userCart.LastModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            await RecalculateCartTotalAndDiscount(userCart);

            return Json(new { success = true, newTotal = userCart.CartItems.Sum(i => i.Quantity * i.Product.Price), newCartCount = userCart.CartItems.Sum(i => i.Quantity) });
        }

        [HttpPost]
        [Authorize] 
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
            await RecalculateCartTotalAndDiscount(userCart);

            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = userCart.CartItems.Sum(i => i.Quantity * i.Product.Price), newCartCount = userCart.CartItems.Sum(i => i.Quantity) });
        }

        [HttpPost]
        [Authorize] 
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
                if (itemToUpdate.Quantity > 1) itemToUpdate.Quantity--;
                else _context.CartItems.Remove(itemToUpdate);
                userCart.LastModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            await RecalculateCartTotalAndDiscount(userCart);

            return Json(new { success = true, newQuantity = itemToUpdate?.Quantity, newItemTotal = itemToUpdate?.Total, newTotal = userCart.CartItems.Sum(i => i.Quantity * i.Product.Price), newCartCount = userCart.CartItems.Sum(i => i.Quantity), itemRemoved = itemToUpdate == null || itemToUpdate.Quantity < 1 });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để áp dụng mã giảm giá." });
            }

            var userCart = await _context.Carts
                                         .Include(c => c.CartItems)
                                             .ThenInclude(ci => ci.Product)
                                         .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart == null || !userCart.CartItems.Any())
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);

            if (coupon == null)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });
            }

            if (coupon.StartDate.HasValue && coupon.StartDate.Value.Date > DateTime.Today.Date)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Mã giảm giá chưa đến ngày có hiệu lực." });
            }
            if (coupon.EndDate.HasValue && coupon.EndDate.Value.Date < DateTime.Today.Date)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
            }

            if (coupon.MaxUses > 0 && coupon.TimesUsed >= coupon.MaxUses)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
            }

            var subtotal = userCart.CartItems.Sum(item => item.Quantity * item.Product.Price);
            if (subtotal < coupon.MinOrderAmount)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = $"Đơn hàng tối thiểu để áp dụng mã này là {coupon.MinOrderAmount.ToString("N0")} đ." });
            }

            bool isApplicableToCartItems = false;
            if (coupon.ProductId.HasValue)
            {
                isApplicableToCartItems = userCart.CartItems.Any(ci => ci.ProductId == coupon.ProductId.Value);
                if (!isApplicableToCartItems)
                {
                    HttpContext.Session.Remove(SessionAppliedCouponCode);
                    HttpContext.Session.Remove(SessionCouponDiscount);
                    return Json(new { success = false, message = "Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng." });
                }
            }
            else if (coupon.CategoryId.HasValue)
            {
                isApplicableToCartItems = userCart.CartItems.Any(ci => ci.Product.CategoryId == coupon.CategoryId.Value);
                if (!isApplicableToCartItems)
                {
                    HttpContext.Session.Remove(SessionAppliedCouponCode);
                    HttpContext.Session.Remove(SessionCouponDiscount);
                    return Json(new { success = false, message = "Mã giảm giá không áp dụng cho danh mục sản phẩm trong giỏ hàng." });
                }
            }
            if (!coupon.ProductId.HasValue && !coupon.CategoryId.HasValue)
            {
                isApplicableToCartItems = true;
            }
            if (!isApplicableToCartItems)
            {
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                return Json(new { success = false, message = "Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng này." });
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = coupon.Value;
            }
            else
            {
                discountAmount = subtotal * (coupon.Value / 100);
            }
            discountAmount = Math.Min(discountAmount, subtotal);

            HttpContext.Session.SetString(SessionAppliedCouponCode, couponCode);
            HttpContext.Session.SetString(SessionCouponDiscount, discountAmount.ToString(CultureInfo.InvariantCulture));

            var newTotal = subtotal - discountAmount;
            return Json(new { success = true, message = "Mã giảm giá đã được áp dụng!", discountAmount = discountAmount.ToString("N0"), newTotal = newTotal.ToString("N0"), appliedCode = couponCode });
        }

        [HttpPost]
        [Authorize]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove(SessionAppliedCouponCode);
            HttpContext.Session.Remove(SessionCouponDiscount);
            return Json(new { success = true, message = "Mã giảm giá đã được gỡ bỏ." });
        }

        private async Task RecalculateCartTotalAndDiscount(Models.Cart userCart)
        {
            var subtotal = userCart.CartItems.Sum(item => item.Quantity * item.Product.Price);
            decimal discountAmount = 0;
            string appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);

            if (!string.IsNullOrEmpty(appliedCouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == appliedCouponCode);
                if (coupon != null && coupon.StartDate.HasValue && coupon.StartDate.Value.Date <= DateTime.Today.Date && (!coupon.EndDate.HasValue || coupon.EndDate.Value.Date >= DateTime.Today.Date) && (coupon.MaxUses == 0 || coupon.TimesUsed < coupon.MaxUses) && subtotal >= coupon.MinOrderAmount)
                {
                    bool isApplicableToCartItems = false;
                    if (coupon.ProductId.HasValue)
                    {
                        isApplicableToCartItems = userCart.CartItems.Any(ci => ci.ProductId == coupon.ProductId.Value);
                    }
                    else if (coupon.CategoryId.HasValue)
                    {
                        isApplicableToCartItems = userCart.CartItems.Any(ci => ci.Product.CategoryId == coupon.CategoryId.Value);
                    }
                    else
                    {
                        isApplicableToCartItems = true;
                    }

                    if (isApplicableToCartItems)
                    {
                        if (coupon.DiscountType == DiscountType.FixedAmount)
                        {
                            discountAmount = coupon.Value;
                        }
                        else
                        {
                            discountAmount = subtotal * (coupon.Value / 100);
                        }
                        discountAmount = Math.Min(discountAmount, subtotal);
                    }
                }
            }
            HttpContext.Session.SetString(SessionCouponDiscount, discountAmount.ToString(CultureInfo.InvariantCulture));
        }
    }
}