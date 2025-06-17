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

namespace DCBStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string BuyNowSessionKey = "BuyNowCart"; 
        private const string CartSessionKey = "Cart"; 

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
                // Người dùng chưa đăng nhập, chỉ có thể có giỏ hàng Session
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

            // Người dùng đã đăng nhập
            List<SessionCartItem> cartItemsForViewModel; 

            // Ưu tiên luồng "Mua ngay" từ Session
            if (HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey) != null)
            {
                cartItemsForViewModel = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey)!;
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
                // Lấy giỏ hàng từ DATABASE cho người dùng đã đăng nhập
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

            if (!cartItemsForViewModel.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var checkoutViewModel = new CheckoutViewModel
            {
                CartItems = cartItemsForViewModel, 
                Order = new Order(), 
                TotalAmount = cartItemsForViewModel.Sum(item => item.Quantity * item.Product.Price) 
            };
            
            return View(checkoutViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel checkoutViewModel)
        {
            Console.WriteLine("PlaceOrder: Bắt đầu xử lý đơn hàng."); // LOG 1

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                Console.WriteLine("PlaceOrder: Lỗi - Người dùng chưa đăng nhập."); // LOG 2
                return Unauthorized();
            }
            Console.WriteLine($"PlaceOrder: Người dùng ID: {userId}"); // LOG 3

            // BẮT ĐẦU SỬA ĐỔI: Tải lại giỏ hàng từ database/session thay vì dùng từ ViewModel
            List<SessionCartItem> currentOrderItems; // Sẽ chứa các item giỏ hàng
            bool isBuyNowFlow = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey) != null;
            
            if (isBuyNowFlow)
            {
                currentOrderItems = HttpContext.Session.GetObjectFromJson<List<SessionCartItem>>(BuyNowSessionKey)!;
                Console.WriteLine("PlaceOrder: Đang xử lý luồng 'Mua ngay'."); // LOG A
            }
            else
            {
                // Tải giỏ hàng từ DATABASE cho người dùng đã đăng nhập (giỏ hàng thông thường)
                var userCart = await _context.Carts
                                             .Include(c => c.CartItems)
                                                 .ThenInclude(ci => ci.Product)
                                                     .ThenInclude(p => p.Images)
                                             .FirstOrDefaultAsync(c => c.UserId == userId);
                
                if (userCart == null)
                {
                    Console.WriteLine("PlaceOrder: Lỗi - Giỏ hàng DB của người dùng trống/không tồn tại cho luồng thông thường."); // LOG B
                    // Nếu giỏ hàng DB trống, có thể có vấn đề, chuyển hướng về giỏ hàng
                    TempData["ErrorMessage"] = "Không tìm thấy giỏ hàng của bạn. Vui lòng thử lại.";
                    return RedirectToAction("Index", "Cart");
                }

                // Ánh xạ CartItem (entity DB) sang SessionCartItem (DTO)
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
                Console.WriteLine("PlaceOrder: Đang xử lý giỏ hàng từ Database."); // LOG C
            }
            // KẾT THÚC SỬA ĐỔI

            Console.WriteLine($"PlaceOrder: Số lượng mặt hàng trong đơn hàng: {currentOrderItems.Count}"); // LOG 4 (đã di chuyển)

            if (!currentOrderItems.Any())
            {
                Console.WriteLine("PlaceOrder: Lỗi - currentOrderItems trống sau khi tải lại."); // LOG 8 (đã cập nhật)
                TempData["ErrorMessage"] = "Không có sản phẩm nào để đặt hàng.";
                return RedirectToAction("Index", "Cart"); 
            }

            var productIds = currentOrderItems.Select(c => c.ProductId).ToList();
            Console.WriteLine($"PlaceOrder: Product IDs trong đơn hàng: {string.Join(",", productIds)}"); // LOG 9
            var productsInDb = await _context.Products
                                             .Where(p => productIds.Contains(p.Id))
                                             .ToListAsync();
            Console.WriteLine($"PlaceOrder: Đã tải {productsInDb.Count} sản phẩm từ DB cho kiểm tra tồn kho."); // LOG 10

            foreach (var item in currentOrderItems)
            {
                var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    Console.WriteLine($"PlaceOrder: Lỗi tồn kho - Sản phẩm {item.ProductName} (ID: {item.ProductId}) không đủ ({item.Quantity} > {product?.Stock})."); // LOG 11
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.Product?.Name ?? "Không xác định"}' không đủ số lượng tồn kho hoặc không còn tồn tại.";
                    return RedirectToAction("Index", "Cart"); 
                }
            }
            Console.WriteLine("PlaceOrder: Kiểm tra tồn kho thành công."); // LOG 12

            using var transaction = await _context.Database.BeginTransactionAsync();
            Console.WriteLine("PlaceOrder: Bắt đầu transaction."); // LOG 13
            try
            {
                var order = checkoutViewModel.Order;
                var user = await _userManager.GetUserAsync(User);

                order.UserId = user?.Id; 
                order.OrderDate = DateTime.UtcNow; 
                order.Total = currentOrderItems.Sum(item => item.Quantity * item.Product.Price); 
                order.Status = OrderStatus.Pending; 

                _context.Orders.Add(order);
                Console.WriteLine("PlaceOrder: Đã thêm Order vào context. Đang lưu Order..."); // LOG 14
                await _context.SaveChangesAsync(); 
                Console.WriteLine($"PlaceOrder: Order ID mới: {order.Id}"); // LOG 15

                foreach (var item in currentOrderItems)
                {
                    var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null) 
                    {
                        product.Stock -= item.Quantity; 
                        Console.WriteLine($"PlaceOrder: Cập nhật tồn kho cho {product.Name}: còn {product.Stock}"); // LOG 16
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price 
                    };
                    _context.OrderDetails.Add(orderDetail);
                    Console.WriteLine($"PlaceOrder: Đã thêm OrderDetail cho Product ID {item.ProductId}"); // LOG 17
                }
                
                Console.WriteLine("PlaceOrder: Đang lưu OrderDetails và cập nhật tồn kho..."); // LOG 18
                await _context.SaveChangesAsync();
                Console.WriteLine("PlaceOrder: Đã lưu OrderDetails và cập nhật tồn kho. Đang commit transaction..."); // LOG 19
                await transaction.CommitAsync();
                Console.WriteLine("PlaceOrder: Transaction committed thành công."); // LOG 20

                // Xóa giỏ hàng sau khi đặt hàng thành công
                if (isBuyNowFlow)
                {
                    HttpContext.Session.Remove(BuyNowSessionKey);
                    Console.WriteLine("PlaceOrder: Đã xóa BuyNowSessionKey."); // LOG 21
                }
                else // Nếu là giỏ hàng thông thường, xóa nó khỏi database
                {
                    var userCart = await _context.Carts
                                                 .Include(c => c.CartItems)
                                                 .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (userCart != null)
                    {
                        _context.CartItems.RemoveRange(userCart.CartItems); 
                        Console.WriteLine($"PlaceOrder: Đã xóa {userCart.CartItems.Count} mục khỏi giỏ hàng DB của user {userId}."); // LOG 22
                        await _context.SaveChangesAsync();
                        Console.WriteLine("PlaceOrder: Đã lưu thay đổi sau khi xóa giỏ hàng DB."); // LOG 23
                    }
                    HttpContext.Session.Remove(CartSessionKey); 
                    Console.WriteLine("PlaceOrder: Đã xóa CartSessionKey."); // LOG 24
                }
                Console.WriteLine("PlaceOrder: Đã hoàn tất xử lý giỏ hàng."); // LOG 25

                TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                Console.WriteLine("PlaceOrder: Chuyển hướng đến Confirmation."); // LOG 26
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PlaceOrder: Xảy ra lỗi trong transaction."); // LOG 27
                Console.Error.WriteLine($"PlaceOrder: Chi tiết lỗi: {ex.ToString()}"); // LOG 28: Ghi toàn bộ Stack Trace
                await transaction.RollbackAsync();
                Console.Error.WriteLine("PlaceOrder: Transaction rolled back."); // LOG 29
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