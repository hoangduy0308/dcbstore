using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Thêm
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
            var wishlistItems = await _context.WishlistItems
                                              .Where(w => w.UserId == userId)
                                              .Include(w => w.Product)
                                              .ThenInclude(p => p.Images)
                                              .ToListAsync();
            
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

            var wishlistItem = await _context.WishlistItems
                                             .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
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
                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = false, message = "Đã xóa khỏi danh sách yêu thích." });
            }
        }

        // GET: /Wishlist/GetWishlistStatus - ĐÃ SỬA LỖI
        [HttpGet]
        public async Task<IActionResult> GetWishlistStatus(string productIds)
        {
             var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productIds))
            {
                // Trả về một mảng rỗng để JavaScript không bị lỗi
                return Ok(new List<int>());
            }

            var ids = productIds.Split(',').Select(int.Parse).ToList();
            var likedProductIds = await _context.WishlistItems
                                                .Where(w => w.UserId == userId && ids.Contains(w.ProductId))
                                                .Select(w => w.ProductId)
                                                .ToListAsync();
            
            // Dùng Ok() thay vì Json() để tránh bộ xử lý JSON mặc định thêm metadata
            return Ok(likedProductIds);
        }
    }
}