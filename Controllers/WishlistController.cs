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
    [Authorize]
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
            // BẮT ĐẦU SỬA ĐỔI: Tải Wishlist của User, sau đó tải WishlistItems của Wishlist đó
            var userWishlist = await _context.Wishlists
                                             .Where(w => w.UserId == userId)
                                             .Include(w => w.WishlistItems)
                                                 .ThenInclude(wi => wi.Product)
                                                     .ThenInclude(p => p.Images)
                                             .FirstOrDefaultAsync();
            
            // Nếu không có wishlist, trả về View với danh sách rỗng
            var wishlistItems = userWishlist?.WishlistItems ?? new List<WishlistItem>();
            // KẾT THÚC SỬA ĐỔI
            
            return View(wishlistItems);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // BẮT ĐẦU SỬA ĐỔI: Tìm Wishlist của người dùng và các WishlistItem của họ
            var userWishlist = await _context.Wishlists
                                             .Include(w => w.WishlistItems)
                                             .FirstOrDefaultAsync(w => w.UserId == userId);

            if (userWishlist == null)
            {
                // Nếu người dùng chưa có Wishlist, tạo mới
                userWishlist = new Wishlist { UserId = userId, CreatedDate = DateTime.Now };
                _context.Wishlists.Add(userWishlist);
                await _context.SaveChangesAsync(); // Lưu để có WishlistId
            }

            var wishlistItem = userWishlist.WishlistItems.FirstOrDefault(wi => wi.ProductId == productId);

            if (wishlistItem == null)
            {
                var newItem = new WishlistItem
                {
                    WishlistId = userWishlist.Id, // Liên kết với Wishlist của người dùng
                    ProductId = productId
                };
                _context.WishlistItems.Add(newItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = true, message = "Đã thêm vào danh sách yêu thích!" });
            }
            else
            {
                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = false, message = "Đã xóa khỏi danh sách yêu thích." });
            }
            // KẾT THÚC SỬA ĐỔI
        }

        // GET: /Wishlist/GetWishlistStatus
        [HttpGet]
        public async Task<IActionResult> GetWishlistStatus(string productIds)
        {
             var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productIds))
            {
                return Ok(new List<int>());
            }

            // BẮT ĐẦU SỬA ĐỔI: Tìm Wishlist của người dùng và WishlistItems của họ
            var userWishlist = await _context.Wishlists
                                             .Include(w => w.WishlistItems)
                                             .FirstOrDefaultAsync(w => w.UserId == userId);
            
            if (userWishlist == null)
            {
                return Ok(new List<int>());
            }

            var ids = productIds.Split(',').Select(int.Parse).ToList();
            var likedProductIds = userWishlist.WishlistItems
                                                .Where(wi => ids.Contains(wi.ProductId))
                                                .Select(wi => wi.ProductId)
                                                .ToList();
            // KẾT THÚC SỬA ĐỔI
            
            return Ok(likedProductIds);
        }
    }
}