using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace DCBStore.Hubs
{
    // Yêu cầu người dùng phải đăng nhập để kết nối vào Hub
    [Authorize]
    public class ChatHub : Hub
    {
        // Dùng một Dictionary tĩnh để lưu trữ ánh xạ giữa UserId và ConnectionId
        // Key: UserId (string), Value: ConnectionId (string)
        // ConcurrentDictionary an toàn hơn cho việc truy cập từ nhiều luồng
        private static readonly ConcurrentDictionary<string, string> UserConnections = new ConcurrentDictionary<string, string>();
        
        // Ghi đè phương thức OnConnectedAsync: Được gọi khi một client kết nối thành công
        public override async Task OnConnectedAsync()
        {
            // Lấy UserId của người dùng vừa kết nối (nhờ [Authorize])
            var userId = Context.UserIdentifier; 

            if (!string.IsNullOrEmpty(userId))
            {
                // Lưu lại ConnectionId của người dùng này
                UserConnections[userId] = Context.ConnectionId;
                
                // Gửi thông báo đến các Admin rằng có người dùng mới kết nối
                // Chúng ta sẽ gửi đến một "Group" tên là "Admins"
                await Clients.Group("Admins").SendAsync("UserConnected", userId, Context.User.Identity.Name);
            }

            // Nếu người dùng là Admin, thêm họ vào Group "Admins"
            if (Context.User.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        // Ghi đè phương thức OnDisconnectedAsync: Được gọi khi một client ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                // Xóa ConnectionId khỏi danh sách khi người dùng thoát
                UserConnections.TryRemove(userId, out _);
                 // Gửi thông báo đến các Admin rằng người dùng đã ngắt kết nối
                await Clients.Group("Admins").SendAsync("UserDisconnected", userId);
            }
            
            if (Context.User.IsInRole("Admin"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Phương thức mới cho người dùng gửi tin nhắn ĐẾN ADMIN
        public async Task SendMessageToAdmin(string message)
        {
            var senderUserId = Context.UserIdentifier;
            var senderName = Context.User.Identity.Name;

            if (!string.IsNullOrEmpty(senderUserId))
            {
                // Gửi tin nhắn này đến tất cả các client trong group "Admins"
                await Clients.Group("Admins").SendAsync("ReceiveMessageFromUser", senderUserId, senderName, message);
            }
        }

        // Phương thức mới cho ADMIN gửi tin nhắn trả lời ĐẾN MỘT NGƯỜI DÙNG CỤ THỂ
        public async Task SendMessageToUser(string targetUserId, string message)
        {
            // Chỉ Admin mới được phép gọi hàm này
            if (Context.User.IsInRole("Admin"))
            {
                // Tìm ConnectionId của người dùng mục tiêu
                if (UserConnections.TryGetValue(targetUserId, out string? connectionId))
                {
                    // Gửi tin nhắn chỉ đến cho client có ConnectionId đó
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", "Admin", message);
                }
            }
        }
    }
}