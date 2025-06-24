using System;
using System.Text;
using System.Threading.Tasks;
using DCBStore.Data;                       // namespace chứa ApplicationUser của bạn
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DCBStore.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Email vừa đăng ký, truyền vào query string
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Nếu đang bật RequireConfirmedAccount, hiển thị link gửi lại
        /// </summary>
        public bool ShowResendLink { get; set; }

        /// <summary>
        /// URL để người dùng bấm xác nhận (chỉ dùng khi còn trong môi trường dev)
        /// </summary>
        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với email '{email}'.");
            }

            Email = email;
            ShowResendLink = _userManager.Options.SignIn.RequireConfirmedAccount;

            if (ShowResendLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                EmailConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);
            }

            return Page();
        }
    }
}
