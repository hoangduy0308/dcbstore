using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DCBStore.Data; // Đảm bảo namespace này đúng
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DCBStore.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // CÁC THUỘC TÍNH CẦN THIẾT CHO GIAO DIỆN
        public string Username { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public int OrderCount { get; set; }
        public int WishlistCount { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Tên đầy đủ")]
            public string FullName { get; set; }

            // THUỘC TÍNH CẦN THIẾT CHO UPLOAD AVATAR
            [Display(Name = "Ảnh đại diện")]
            public IFormFile AvatarFile { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            var userWithStats = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Wishlist)
                    .ThenInclude(w => w.WishlistItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (userWithStats != null)
            {
                OrderCount = userWithStats.Orders?.Count ?? 0;
                WishlistCount = userWithStats.Wishlist?.WishlistItems?.Count ?? 0;
            }

            Username = userName;
            Email = email;
            AvatarUrl = user.AvatarUrl;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = user.FullName
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Input.AvatarFile != null)
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/avatars");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.AvatarFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(fileStream);
                }
                user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            }
            
            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName;
            }
            
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Lỗi không mong muốn khi cập nhật số điện thoại.";
                    return RedirectToPage();
                }
            }

            await _userManager.UpdateAsync(user);

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Hồ sơ của bạn đã được cập nhật";
            return RedirectToPage();
        }
    }
}