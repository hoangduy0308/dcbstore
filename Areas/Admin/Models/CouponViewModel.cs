using DCBStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace DCBStore.Areas.Admin.Models
{
    public class CouponViewModel
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
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
        [Display(Name = "Giá trị")]
        public decimal Value { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; } = DateTime.Today;

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Số lần sử dụng tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lần sử dụng không thể âm.")]
        public int MaxUses { get; set; } = 0;

        public int TimesUsed { get; set; } // DÒNG NÀY RẤT QUAN TRỌNG

        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu không thể âm.")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Display(Name = "Áp dụng cho sản phẩm")]
        public int? ProductId { get; set; } 
        [Display(Name = "Sản phẩm")]
        public SelectList? Products { get; set; }

        [Display(Name = "Áp dụng cho danh mục")]
        public int? CategoryId { get; set; } 
        [Display(Name = "Danh mục")]
        public SelectList? Categories { get; set; }

        [Display(Name = "Chỉ áp dụng một lần mỗi người dùng")]
        public bool OneTimePerUser { get; set; } = false;
    }
}