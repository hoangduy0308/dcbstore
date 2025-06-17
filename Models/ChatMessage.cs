using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; 
using DCBStore.Data; 

namespace DCBStore.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }

        // BẮT ĐẦU THÊM MỚI: Thêm ReceiverId để xác định người nhận cụ thể (có thể là null cho tin nhắn chung)
        public string? ReceiverId { get; set; } 
        [ForeignKey("ReceiverId")]
        public ApplicationUser? Receiver { get; set; }
        // KẾT THÚC THÊM MỚI

        public bool IsAdminMessage { get; set; } = false;
    }
}