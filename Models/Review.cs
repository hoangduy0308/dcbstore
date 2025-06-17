using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCBStore.Data; // Để tham chiếu ApplicationUser

namespace DCBStore.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự.")]
        public string Comment { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao.")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
        public int Rating { get; set; } // Điểm đánh giá từ 1 đến 5 sao

        [Required]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow; // Ngày đánh giá

        // Khóa ngoại đến người dùng (người đánh giá)
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        // Khóa ngoại đến sản phẩm được đánh giá
        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}