using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCBStore.Models
{
    public enum DiscountType
    {
        Percentage, // Giảm theo phần trăm (ví dụ: 10%)
        FixedAmount // Giảm theo số tiền cố định (ví dụ: 50.000 VNĐ)
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã giảm giá là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã giảm giá không được vượt quá 50 ký tự.")]
        [Display(Name = "Mã giảm giá")]
        // Khởi tạo để tránh warning CS8618
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc.")]
        [Display(Name = "Loại giảm giá")]
        public DiscountType DiscountType { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá trị")]
        // Tên thuộc tính trong Controller sẽ dùng là "Value"
        public decimal Value { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Số lần sử dụng tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lần sử dụng không thể âm.")]
        public int MaxUses { get; set; } = 0;

        [Display(Name = "Số lần đã sử dụng")]
        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu không thể âm.")]
        [Column(TypeName = "decimal(18, 2)")]
         // Tên thuộc tính trong Controller sẽ dùng là "MinOrderAmount"
        public decimal MinOrderAmount { get; set; } = 0;

        // === BẮT ĐẦU PHẦN THÊM MỚI ===
        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true; // Thêm thuộc tính để bật/tắt mã

        [Display(Name = "Giảm giá tối đa")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa không thể âm.")]
        [Column(TypeName = "decimal(18, 2)")]
        // Thêm thuộc tính giới hạn số tiền giảm (0 = không giới hạn)
        public decimal MaxDiscountAmount { get; set; } = 0;
        // === KẾT THÚC PHẦN THÊM MỚI ===

        [Display(Name = "Áp dụng cho sản phẩm")]
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Display(Name = "Áp dụng cho danh mục")]
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Chỉ áp dụng một lần mỗi người dùng")]
        public bool OneTimePerUser { get; set; } = false;
    }
}