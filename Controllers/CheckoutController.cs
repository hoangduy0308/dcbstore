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
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
// SỬA LỖI: Thêm using cho IEmailSender
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DCBStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // SỬA LỖI: Thay đổi từ class cụ thể sang interface
        private readonly IEmailSender _emailSender; 
        private const string BuyNowSessionKey = "BuyNowItem";
        private const string SessionAppliedCouponCode = "AppliedCouponCode";
        private const string SessionCouponDiscount = "CouponDiscount";

        // SỬA LỖI: Yêu cầu IEmailSender trong constructor
        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(bool fromCart = false)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            if (fromCart)
            {
                HttpContext.Session.Remove(BuyNowSessionKey);
            }

            var cartItems = await GetCurrentCartItemsAsync(userId);

            if (cartItems == null || !cartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            decimal subtotal = cartItems.Sum(item => item.Quantity * item.Price);
            decimal discount = 0;
            string? appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);

            if (!string.IsNullOrEmpty(appliedCouponCode))
            {
                discount = decimal.Parse(HttpContext.Session.GetString(SessionCouponDiscount) ?? "0", CultureInfo.InvariantCulture);
            }
            
            discount = Math.Min(discount, subtotal);

            var checkoutViewModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                Order = new Order(),
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalAmount = subtotal - discount,
                AppliedCouponCode = appliedCouponCode,
            };

            return View(checkoutViewModel);
        }
        
        [HttpPost]
        public async Task<JsonResult> ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            var cartItems = await GetCurrentCartItemsAsync(userId);
            if (cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });
            }

            var subtotal = cartItems.Sum(item => item.Quantity * item.Price);
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper());

            if (coupon == null || !coupon.IsActive)
            {
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });
            }
            if ((coupon.StartDate.HasValue && DateTime.Now < coupon.StartDate) || (coupon.EndDate.HasValue && DateTime.Now > coupon.EndDate))
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn hoặc chưa có hiệu lực." });
            }
            if (coupon.MaxUses > 0 && coupon.TimesUsed >= coupon.MaxUses)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
            }
            if (subtotal < coupon.MinOrderAmount)
            {
                return Json(new { success = false, message = $"Đơn hàng tối thiểu phải là {coupon.MinOrderAmount:N0} đ để áp dụng mã này." });
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
            
            if(coupon.MaxDiscountAmount > 0)
            {
                discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount);
            }

            var newTotal = subtotal - discountAmount;

            HttpContext.Session.SetString(SessionAppliedCouponCode, coupon.Code);
            HttpContext.Session.SetString(SessionCouponDiscount, discountAmount.ToString(CultureInfo.InvariantCulture));

            return Json(new
            {
                success = true,
                message = "Áp dụng mã giảm giá thành công!",
                subtotal = subtotal.ToString("N0"),
                discountAmount = discountAmount.ToString("N0"),
                newTotal = newTotal.ToString("N0"),
                appliedCouponCode = coupon.Code
            });
        }
        
        [HttpPost]
        public async Task<JsonResult> RemoveCoupon()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            
            var cartItems = await GetCurrentCartItemsAsync(userId);
            if (cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống." });
            }
            var subtotal = cartItems.Sum(item => item.Quantity * item.Price);
            HttpContext.Session.Remove(SessionAppliedCouponCode);
            HttpContext.Session.Remove(SessionCouponDiscount);

            return Json(new {
                success = true,
                message = "Đã xóa mã giảm giá.",
                newTotal = subtotal.ToString("N0")
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel checkoutViewModel)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User); 
            if (userId == null || user == null) return Unauthorized();

            var currentOrderItems = await GetCurrentCartItemsAsync(userId);
            if (currentOrderItems == null || !currentOrderItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào để đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var productIds = currentOrderItems.Select(c => c.ProductId).ToList();
            var productsInDb = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var item in currentOrderItems)
            {
                var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' không đủ số lượng tồn kho.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = checkoutViewModel.Order;
                order.UserId = userId;
                order.OrderDate = DateTime.UtcNow;
                order.Status = OrderStatus.Pending;

                decimal subtotal = currentOrderItems.Sum(item => item.Quantity * item.Price);
                decimal discountAmount = decimal.Parse(HttpContext.Session.GetString(SessionCouponDiscount) ?? "0", CultureInfo.InvariantCulture);
                string? appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);
                
                discountAmount = Math.Min(subtotal, discountAmount);
                order.Total = subtotal - discountAmount;
                
                if (!string.IsNullOrEmpty(appliedCouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == appliedCouponCode);
                    if (coupon != null && (coupon.MaxUses == 0 || coupon.TimesUsed < coupon.MaxUses))
                    {
                        coupon.TimesUsed++;
                        _context.Coupons.Update(coupon);
                    }
                }
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                var orderDetailsHtml = new StringBuilder();
                foreach (var item in currentOrderItems)
                {
                    var product = productsInDb.First(p => p.Id == item.ProductId);
                    product.Stock -= item.Quantity;
                    product.SoldQuantity += item.Quantity;
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                    orderDetailsHtml.Append($"<tr><td style='padding: 8px; border: 1px solid #ddd;'>{item.ProductName}</td><td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{item.Quantity}</td><td style='padding: 8px; border: 1px solid #ddd; text-align: right;'>{item.Price:N0} đ</td></tr>");
                }
                
                await _context.SaveChangesAsync();
                
                string subject = $"DCBStore - Xác nhận đơn hàng #{order.Id}";
                string body = $@"
                    <h1>Cảm ơn bạn đã đặt hàng!</h1>
                    <p>Chào {order.CustomerName},</p>
                    <p>Đơn hàng #{order.Id} của bạn đã được tiếp nhận và đang được xử lý.</p>
                    <h3>Chi tiết đơn hàng:</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th style='padding: 8px; border: 1px solid #ddd; background-color: #f2f2f2; text-align: left;'>Sản phẩm</th>
                                <th style='padding: 8px; border: 1px solid #ddd; background-color: #f2f2f2; text-align: center;'>Số lượng</th>
                                <th style='padding: 8px; border: 1px solid #ddd; background-color: #f2f2f2; text-align: right;'>Đơn giá</th>
                            </tr>
                        </thead>
                        <tbody>
                            {orderDetailsHtml}
                        </tbody>
                    </table>
                    <h3 style='text-align: right;'>Tổng cộng: {order.Total:N0} đ</h3>
                    <p>Chúng tôi sẽ thông báo cho bạn khi đơn hàng được vận chuyển.</p>
                    <p>Cảm ơn bạn đã tin tưởng DCBStore!</p>";
                
                await _emailSender.SendEmailAsync(user.Email, subject, body);
                
                await transaction.CommitAsync();

                bool isBuyNowFlow = HttpContext.Session.Keys.Contains(BuyNowSessionKey);
                if (isBuyNowFlow)
                {
                    HttpContext.Session.Remove(BuyNowSessionKey);
                }
                else
                {
                    var userCart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId);
                    if (userCart != null)
                    {
                        _context.CartItems.RemoveRange(userCart.CartItems);
                        await _context.SaveChangesAsync();
                    }
                }
                
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);

                TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.ToString()); 
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình xử lý đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }
        }
        
        public IActionResult Confirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
        
        private async Task<List<SessionCartItem>?> GetCurrentCartItemsAsync(string userId)
        {
            var buyNowItem = HttpContext.Session.GetObjectFromJson<SessionCartItem>(BuyNowSessionKey);
            if (buyNowItem != null)
            {
                if (buyNowItem.Product == null)
                {
                    buyNowItem.Product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == buyNowItem.ProductId);
                    if (buyNowItem.Product == null) return null;
                }
                return new List<SessionCartItem> { buyNowItem };
            }

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
    }
}