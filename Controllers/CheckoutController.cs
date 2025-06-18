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
using System.Globalization; // Cần thêm để sử dụng CultureInfo

namespace DCBStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string BuyNowSessionKey = "BuyNowCart"; 
        private const string CartSessionKey = "CartSession"; // Đảm bảo key này khớp với CartController
        private const string SessionAppliedCouponCode = "AppliedCouponCode"; // Đảm bảo key này khớp với CartController
        private const string SessionCouponDiscount = "CouponDiscount"; // Đảm bảo key này khớp với CartController

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                var sessionCartItems = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(CartSessionKey) ?? new List<SessionCartItem>();
                
                foreach (var item in sessionCartItems)
                {
                    if (item.Product == null)
                    {
                        var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            item.Product = product;
                            item.ProductName = product.Name;
                            item.Price = product.Price;
                            item.ImageUrl = product.Images?.FirstOrDefault()?.Url;
                        }
                    }
                }
                var anonCheckoutViewModel = new CheckoutViewModel
                {
                    CartItems = sessionCartItems,
                    Order = new Order(),
                    TotalAmount = sessionCartItems.Sum(item => item.Quantity * item.Product.Price)
                };
                return View(anonCheckoutViewModel);
            }

            List<SessionCartItem> cartItemsForViewModel; 
            decimal subtotal = 0;
            decimal discount = 0;
            string appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);


            if (HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey) != null)
            {
                cartItemsForViewModel = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey);
                foreach (var item in cartItemsForViewModel)
                {
                    if (item.Product == null)
                    {
                        var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            item.Product = product;
                            item.ProductName = product.Name;
                            item.Price = product.Price;
                            item.ImageUrl = product.Images?.FirstOrDefault()?.Url;
                        }
                    }
                }
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

                cartItemsForViewModel = new List<SessionCartItem>();
                if(userCart != null)
                {
                    foreach (var dbItem in userCart.CartItems)
                    {
                        cartItemsForViewModel.Add(new SessionCartItem
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

            if (!cartItemsForViewModel.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Tính toán Subtotal
            subtotal = cartItemsForViewModel.Sum(item => item.Quantity * item.Product.Price);

            // Lấy discount từ session nếu có
            if (HttpContext.Session.TryGetValue(SessionCouponDiscount, out byte[] discountBytes))
            {
                discount = decimal.Parse(System.Text.Encoding.UTF8.GetString(discountBytes), CultureInfo.InvariantCulture);
            }

            // Đảm bảo discount không lớn hơn subtotal
            discount = Math.Min(discount, subtotal);

            var checkoutViewModel = new CheckoutViewModel
            {
                CartItems = cartItemsForViewModel, 
                Order = new Order(), 
                TotalAmount = subtotal - discount, // Tổng tiền đã trừ giảm giá
                AppliedCouponCode = appliedCouponCode, // Mã giảm giá đã áp dụng
                DiscountAmount = discount // Số tiền giảm giá
            };
            
            return View(checkoutViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel checkoutViewModel)
        {
            // LOGGING CHO DEBUGGING
            Console.WriteLine("PlaceOrder: Bắt đầu xử lý đơn hàng.");

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                Console.WriteLine("PlaceOrder: Lỗi - Người dùng chưa đăng nhập.");
                return Unauthorized();
            }
            Console.WriteLine($"PlaceOrder: Người dùng ID: {userId}");

            List<SessionCartItem> currentOrderItems;
            bool isBuyNowFlow = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey) != null;
            
            if (isBuyNowFlow)
            {
                currentOrderItems = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey);
                Console.WriteLine("PlaceOrder: Đang xử lý luồng 'Mua ngay'.");
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
                    Console.WriteLine("PlaceOrder: Lỗi - Giỏ hàng DB của người dùng trống/không tồn tại cho luồng thông thường.");
                    TempData["ErrorMessage"] = "Không tìm thấy giỏ hàng của bạn. Vui lòng thử lại.";
                    return RedirectToAction("Index", "Cart");
                }

                currentOrderItems = new List<SessionCartItem>();
                foreach (var dbItem in userCart.CartItems)
                {
                    currentOrderItems.Add(new SessionCartItem
                    {
                        ProductId = dbItem.ProductId,
                        ProductName = dbItem.Product.Name,
                        Price = dbItem.Product.Price,
                        Quantity = dbItem.Quantity,
                        ImageUrl = dbItem.Product.Images?.FirstOrDefault()?.Url,
                        Product = dbItem.Product
                    });
                }
                Console.WriteLine("PlaceOrder: Đang xử lý giỏ hàng từ Database.");
            }

            Console.WriteLine($"PlaceOrder: Số lượng mặt hàng trong đơn hàng: {currentOrderItems.Count}");

            if (!currentOrderItems.Any())
            {
                Console.WriteLine("PlaceOrder: Lỗi - currentOrderItems trống sau khi tải lại.");
                TempData["ErrorMessage"] = "Không có sản phẩm nào để đặt hàng.";
                return RedirectToAction("Index", "Cart"); 
            }

            var productIds = currentOrderItems.Select(c => c.ProductId).ToList();
            Console.WriteLine($"PlaceOrder: Product IDs trong đơn hàng: {string.Join(",", productIds)}");
            var productsInDb = await _context.Products
                                             .Where(p => productIds.Contains(p.Id))
                                             .ToListAsync();
            Console.WriteLine($"PlaceOrder: Đã tải {productsInDb.Count} sản phẩm từ DB cho kiểm tra tồn kho.");

            foreach (var item in currentOrderItems)
            {
                var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    Console.WriteLine($"PlaceOrder: Lỗi tồn kho - Sản phẩm {item.ProductName} (ID: {item.ProductId}) không đủ ({item.Quantity} > {product?.Stock}).");
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.Product?.Name ?? "Không xác định"}' không đủ số lượng tồn kho hoặc không còn tồn tại.";
                    return RedirectToAction("Index", "Cart"); 
                }
            }
            Console.WriteLine("PlaceOrder: Kiểm tra tồn kho thành công.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            Console.WriteLine("PlaceOrder: Bắt đầu transaction.");
            try
            {
                var order = checkoutViewModel.Order;
                var user = await _userManager.GetUserAsync(User);

                order.UserId = user?.Id; 
                order.OrderDate = DateTime.UtcNow; 
                order.Status = OrderStatus.Pending; 
                
                // BẮT ĐẦU THÊM MỚI: Áp dụng discount từ session vào Total của Order
                decimal finalOrderTotal = currentOrderItems.Sum(item => item.Quantity * item.Product.Price);
                decimal discountAmount = 0;
                string appliedCouponCode = HttpContext.Session.GetString(SessionAppliedCouponCode);

                if (!string.IsNullOrEmpty(appliedCouponCode))
                {
                    if (decimal.TryParse(HttpContext.Session.GetString(SessionCouponDiscount), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDiscount))
                    {
                        discountAmount = parsedDiscount;
                    }
                }
                finalOrderTotal = Math.Max(0, finalOrderTotal - discountAmount); // Đảm bảo tổng tiền không âm
                order.Total = finalOrderTotal;

                // Tăng TimesUsed của coupon nếu có và hợp lệ
                if (!string.IsNullOrEmpty(appliedCouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == appliedCouponCode);
                    if (coupon != null && (coupon.MaxUses == 0 || coupon.TimesUsed < coupon.MaxUses))
                    {
                        coupon.TimesUsed++;
                        _context.Coupons.Update(coupon);
                        await _context.SaveChangesAsync(); // Lưu thay đổi TimesUsed
                        Console.WriteLine($"PlaceOrder: Mã giảm giá '{appliedCouponCode}' đã tăng lượt sử dụng lên {coupon.TimesUsed}.");
                    }
                }
                // KẾT THÚC THÊM MỚI


                _context.Orders.Add(order);
                Console.WriteLine("PlaceOrder: Đã thêm Order vào context. Đang lưu Order...");
                await _context.SaveChangesAsync(); 
                Console.WriteLine($"PlaceOrder: Order ID mới: {order.Id}");

                foreach (var item in currentOrderItems)
                {
                    var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null) 
                    {
                        product.Stock -= item.Quantity; 
                        Console.WriteLine($"PlaceOrder: Cập nhật tồn kho cho {product.Name}: còn {product.Stock}");
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price 
                    };
                    _context.OrderDetails.Add(orderDetail);
                    Console.WriteLine($"PlaceOrder: Đã thêm OrderDetail cho Product ID {item.ProductId}");
                }
                
                Console.WriteLine("PlaceOrder: Đang lưu OrderDetails và cập nhật tồn kho...");
                await _context.SaveChangesAsync();
                Console.WriteLine("PlaceOrder: Đã lưu OrderDetails và cập nhật tồn kho. Đang commit transaction...");
                await transaction.CommitAsync();
                Console.WriteLine("PlaceOrder: Transaction committed thành công.");

                if (isBuyNowFlow)
                {
                    HttpContext.Session.Remove(BuyNowSessionKey);
                    Console.WriteLine("PlaceOrder: Đã xóa BuyNowSessionKey.");
                }
                else
                {
                    var userCart = await _context.Carts
                                                 .Include(c => c.CartItems)
                                                 .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (userCart != null)
                    {
                        _context.CartItems.RemoveRange(userCart.CartItems); 
                        Console.WriteLine($"PlaceOrder: Đã xóa {userCart.CartItems.Count} mục khỏi giỏ hàng DB của user {userId}.");
                        await _context.SaveChangesAsync();
                        Console.WriteLine("PlaceOrder: Đã lưu thay đổi sau khi xóa giỏ hàng DB.");
                    }
                    HttpContext.Session.Remove(CartSessionKey); 
                    Console.WriteLine("PlaceOrder: Đã xóa CartSessionKey.");
                }
                Console.WriteLine("PlaceOrder: Đã hoàn tất xử lý giỏ hàng.");

                TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                Console.WriteLine("PlaceOrder: Chuyển hướng đến Confirmation.");
                
                // BẮT ĐẦU THÊM MỚI: Xóa thông tin coupon khỏi session sau khi đặt hàng thành công
                HttpContext.Session.Remove(SessionAppliedCouponCode);
                HttpContext.Session.Remove(SessionCouponDiscount);
                // KẾT THÚC THÊM MỚI

                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PlaceOrder: Xảy ra lỗi trong transaction.");
                Console.Error.WriteLine($"PlaceOrder: Chi tiết lỗi: {ex.ToString()}");
                await transaction.RollbackAsync();
                Console.Error.WriteLine("PlaceOrder: Transaction rolled back.");
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình xử lý đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart"); 
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để mua ngay." });
            }

            var product = await _context.Products
                                         .Include(p => p.Images) 
                                         .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm phải lớn hơn 0." });
            }

            if (product.Stock < quantity)
            {
                return Json(new { success = false, message = "Sản phẩm không đủ số lượng trong kho." });
            }

            var buyNowItem = new SessionCartItem
            {
                ProductId = productId,
                Quantity = quantity,
                ProductName = product.Name, 
                Price = product.Price,
                ImageUrl = product.Images?.FirstOrDefault()?.Url,
                Product = product, 
                UserId = userId 
            };

            HttpContext.Session.SetObjectAsJson(BuyNowSessionKey, new List<SessionCartItem> { buyNowItem }); 

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Checkout") });
        }

        public IActionResult Confirmation(int orderId)
        {
            ViewBag.OrderId = orderId; 
            return View();
        }
    }
}