using DCBStore.Data;
using DCBStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DCBStore.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize] // Yêu cầu người dùng đăng nhập để gửi đánh giá
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, string comment, int rating)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
                return RedirectToAction("Login", "Account", new { area = "Identity" }); // Chuyển hướng đến trang đăng nhập
            }

            // Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa (tùy chọn)
            // var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
            // if (existingReview != null)
            // {
            //     TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
            //     return RedirectToAction("Details", "Products", new { id = productId });
            // }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Comment = comment,
                Rating = rating,
                ReviewDate = DateTime.UtcNow
            };

            if (ModelState.IsValid)
            {
                _context.Add(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}