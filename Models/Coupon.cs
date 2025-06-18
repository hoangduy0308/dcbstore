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
        public string Code { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc.")]
        [Display(Name = "Loại giảm giá")]
        public DiscountType DiscountType { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá trị")]
        public decimal Value { get; set; } // Giá trị giảm giá (ví dụ: 10.00 cho 10%, hoặc 50000 cho 50.000 VNĐ)

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; } = DateTime.UtcNow; // Ngày bắt đầu có hiệu lực

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; } // Ngày hết hạn

        [Display(Name = "Số lần sử dụng tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lần sử dụng không thể âm.")]
        public int MaxUses { get; set; } = 0; // Số lần sử dụng tối đa (0 = không giới hạn)

        [Display(Name = "Số lần đã sử dụng")]
        public int TimesUsed { get; set; } = 0; // Số lần đã được sử dụng

        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu không thể âm.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MinOrderAmount { get; set; } = 0; // Giá trị đơn hàng tối thiểu để áp dụng

        [Display(Name = "Áp dụng cho sản phẩm")]
        public int? ProductId { get; set; } // Áp dụng cho sản phẩm cụ thể (nullable)
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Display(Name = "Áp dụng cho danh mục")]
        public int? CategoryId { get; set; } // Áp dụng cho danh mục cụ thể (nullable)
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Chỉ áp dụng một lần mỗi người dùng")]
        public bool OneTimePerUser { get; set; } = false; // Mã giảm giá chỉ dùng được 1 lần/người dùng
    }
}