using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DCBStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<IdentityUser> signInManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // Class này định nghĩa các trường trong form đăng nhập
        public class LoginInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email.")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ đăng nhập?")]
            public bool RememberMe { get; set; }
        }

        // GET: /Admin/Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Admin/Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel input, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Admin");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
                    return View(input);
                }
            }
            return View(input);
        }
        
        // POST: /Admin/Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            // Chuyển hướng về trang đăng nhập của admin sau khi logout
            return RedirectToAction("Login", "Account", new { Area = "Admin" });
        }
    }
}
