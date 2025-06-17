using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DCBStore.Data;
using DCBStore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace DCBStore.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new ConcurrentDictionary<string, string>();
        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; 
            var userName = Context.User.Identity.Name;

            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                
                await Clients.Group("Admins").SendAsync("UserConnected", userId, userName);

                await LoadChatHistoryForCurrentUser(userId);
            }

            if (Context.User.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _);
                await Clients.Group("Admins").SendAsync("UserDisconnected", userId);
            }
            
            if (Context.User.IsInRole("Admin"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToAdmin(string message)
        {
            var senderUserId = Context.UserIdentifier;
            var senderName = Context.User.Identity.Name;
            var isAdminSender = Context.User.IsInRole("Admin");

            if (!string.IsNullOrEmpty(senderUserId) && senderName != null)
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = senderUserId,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsAdminMessage = isAdminSender,
                    ReceiverId = null
                };

                await _context.ChatMessages.AddAsync(chatMessage);
                await _context.SaveChangesAsync();

                await Clients.Group("Admins").SendAsync("ReceiveMessageFromUser", senderUserId, senderName, message, chatMessage.Timestamp, chatMessage.IsAdminMessage);
                await Clients.Caller.SendAsync("ReceiveMessage", senderName, message, chatMessage.Timestamp, senderUserId, isAdminSender);
            }
        }

        public async Task SendMessageToUser(string targetUserId, string message)
        {
            if (Context.User.IsInRole("Admin"))
            {
                var adminUserId = Context.UserIdentifier;
                var adminUserName = Context.User.Identity.Name;

                var chatMessage = new ChatMessage
                {
                    SenderId = adminUserId,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsAdminMessage = true,
                    ReceiverId = targetUserId
                };

                await _context.ChatMessages.AddAsync(chatMessage);
                await _context.SaveChangesAsync();

                if (UserConnections.TryGetValue(targetUserId, out string? connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", adminUserName, message, chatMessage.Timestamp, adminUserId, chatMessage.IsAdminMessage);
                }
                
                await Clients.Group("Admins").SendAsync("ReceiveMessageFromUser", adminUserId, adminUserName, message, chatMessage.Timestamp, chatMessage.IsAdminMessage);
            }
        }

        private async Task LoadChatHistoryForCurrentUser(string userId)
        {
            var messagesQuery = _context.ChatMessages
                                         .Include(m => m.Sender)
                                         .AsQueryable();

            if (!Context.User.IsInRole("Admin"))
            {
                messagesQuery = messagesQuery.Where(m => m.SenderId == userId || (m.IsAdminMessage && m.ReceiverId == userId));
            }

            var messages = await messagesQuery
                                         .OrderByDescending(m => m.Timestamp)
                                         .Take(50)
                                         .OrderBy(m => m.Timestamp)
                                         .ToListAsync();

            foreach (var msg in messages)
            {
                if (msg.IsAdminMessage)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", msg.Sender.UserName, msg.Message, msg.Timestamp, msg.SenderId, msg.IsAdminMessage);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveMessageFromUser", msg.SenderId, msg.Sender.UserName, msg.Message, msg.Timestamp, msg.IsAdminMessage);
                }
            }
        }

        public async Task LoadSpecificUserChatHistory(string targetUserId)
        {
            if (!Context.User.IsInRole("Admin"))
            {
                return; 
            }

            var messages = await _context.ChatMessages
                                 .Include(m => m.Sender)
                                 // Lọc tin nhắn để hiển thị cuộc trò chuyện giữa người dùng mục tiêu và admin:
                                 // 1. Tin nhắn từ người dùng mục tiêu gửi cho admin (ReceiverId == null)
                                 // HOẶC
                                 // 2. Tin nhắn admin gửi ĐẾN người dùng mục tiêu (ReceiverId == targetUserId)
                                 .Where(m => (m.SenderId == targetUserId && m.ReceiverId == null)
                                          || (m.IsAdminMessage && m.ReceiverId == targetUserId))
                                 .OrderBy(m => m.Timestamp)
                                 .Take(100)
                                 .ToListAsync();

            foreach (var msg in messages)
            {
                if (msg.IsAdminMessage)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", msg.Sender.UserName, msg.Message, msg.Timestamp, msg.SenderId, msg.IsAdminMessage);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveMessageFromUser", msg.SenderId, msg.Sender.UserName, msg.Message, msg.Timestamp, msg.IsAdminMessage);
                }
            }
        }
    }
}