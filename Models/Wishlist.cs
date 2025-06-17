// Models/Wishlist.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCBStore.Data; // Để nhận diện ApplicationUser

namespace DCBStore.Models
{
    public class Wishlist
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại đến người dùng (ApplicationUser)
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!; // Navigation property

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Danh sách các mặt hàng trong danh sách yêu thích
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }
}