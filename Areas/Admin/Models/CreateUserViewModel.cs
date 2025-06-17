using System.ComponentModel.DataAnnotations;

namespace DCBStore.Areas.Admin.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Tên đầy đủ")]
        [StringLength(255)]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Vai trò")]
        // Bạn có thể dùng List<string> cho nhiều vai trò, hoặc string cho một vai trò chính
        public string Role { get; set; } = "Customer"; // Mặc định là Customer
    }
}