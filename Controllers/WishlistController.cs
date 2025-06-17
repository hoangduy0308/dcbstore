using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DCBStore.Controllers
{
    [Authorize] // Đảm bảo người dùng đã đăng nhập để truy cập các hành động trong controller này
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlistItems = await _context.WishlistItems
                                              .Where(w => w.UserId == userId)
                                              .Include(w => w.Product)
                                              .ThenInclude(p => p.Images)
                                              .ToListAsync();
            
            return View(wishlistItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Đảm bảo xác thực Anti-Forgery Token
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                // Trả về thông báo lỗi nếu người dùng chưa đăng nhập
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này." });
            }

            var wishlistItem = await _context.WishlistItems
                                             .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
                // Thêm sản phẩm vào danh sách yêu thích
                var newItem = new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.WishlistItems.Add(newItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = true, message = "Đã thêm vào danh sách yêu thích!" });
            }
            else
            {
                // Xóa sản phẩm khỏi danh sách yêu thích
                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = false, message = "Đã xóa khỏi danh sách yêu thích." });
            }
        }

        // GET: /Wishlist/GetWishlistStatus
        // Đã sửa lỗi: Trả về trạng thái yêu thích của các sản phẩm trên trang
        [HttpGet]
        public async Task<IActionResult> GetWishlistStatus(string productIds)
        {
             var userId = _userManager.GetUserId(User);
            // Nếu người dùng chưa đăng nhập hoặc không có productIds, trả về mảng rỗng để JavaScript không bị lỗi
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productIds))
            {
                return Ok(new List<int>());
            }

            // Phân tích chuỗi productIds thành danh sách các số nguyên, loại bỏ các mục rỗng
            var ids = productIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse)
                                .ToList();

            // Truy vấn cơ sở dữ liệu để lấy các ProductId mà người dùng đã yêu thích
            var likedProductIds = await _context.WishlistItems
                                                .Where(w => w.UserId == userId && ids.Contains(w.ProductId))
                                                .Select(w => w.ProductId)
                                                .ToListAsync();
            
            // Dùng Ok() để trả về một mảng JSON các số nguyên
            return Ok(likedProductIds);
        }
    }
}