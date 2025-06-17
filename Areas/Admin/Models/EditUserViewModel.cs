using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DCBStore.Areas.Admin.Models
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } // ID người dùng cần chỉnh sửa

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Tên đầy đủ")]
        [StringLength(255)]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự nếu muốn thay đổi.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; } // Mật khẩu mới (tùy chọn)

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string? ConfirmNewPassword { get; set; } // Xác nhận mật khẩu mới

        [Display(Name = "Vai trò hiện tại")]
        public IList<string> CurrentRoles { get; set; } = new List<string>(); // Vai trò hiện tại của người dùng

        [Display(Name = "Chọn vai trò")]
        public string SelectedRole { get; set; } // Vai trò được chọn từ dropdown (cho mục đích demo 1 vai trò chính)
        // Nếu muốn hỗ trợ nhiều vai trò cùng lúc, có thể dùng List<string> SelectedRoles {get; set;}
    }
}