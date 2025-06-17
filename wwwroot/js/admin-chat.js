"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

// Lấy các element trên trang admin
const userList = document.getElementById("user-list");
const chatPanel = document.getElementById("chat-panel");
const selectChatPrompt = document.getElementById("select-chat-prompt");
const adminChatMessages = document.getElementById("admin-chat-messages");
const adminChatForm = document.getElementById("admin-chat-form");
const adminMessageInput = document.getElementById("admin-message-input");
const chatWithUsername = document.getElementById("chat-with-username");
const targetUserIdInput = document.getElementById("target-user-id");
const noUsersMessage = document.getElementById("no-users-message");

let activeUserId = null;

// Hàm để thêm user vào danh sách
function addUserToList(userId, username) {
    if (noUsersMessage) noUsersMessage.style.display = 'none';

    // Tránh thêm trùng lặp
    if (document.getElementById(`user-${userId}`)) return;

    const li = document.createElement("li");
    li.className = "list-group-item list-group-item-action";
    li.id = `user-${userId}`;
    li.dataset.userid = userId;
    li.dataset.username = username;
    li.innerHTML = `<div>${username}</div><span class="badge bg-danger rounded-pill" style="display:none;"></span>`;
    userList.appendChild(li);

    li.addEventListener('click', function() {
        if(activeUserId === userId) return;

        // Bỏ active user cũ
        const currentActive = document.querySelector('#user-list .list-group-item.active');
        if(currentActive) currentActive.classList.remove('active');

        // Active user mới
        li.classList.add('active');
        li.querySelector('.badge').style.display = 'none'; // Ẩn thông báo tin nhắn mới

        // Hiển thị khung chat
        chatPanel.style.display = 'block';
        selectChatPrompt.style.display = 'none';
        
        chatWithUsername.textContent = username;
        targetUserIdInput.value = userId;
        activeUserId = userId;
        adminChatMessages.innerHTML = ''; // Xóa tin nhắn cũ (sẽ nâng cấp để load lịch sử sau)
    });
}

// Hàm để xóa user khỏi danh sách
function removeUserFromList(userId) {
    const userElement = document.getElementById(`user-${userId}`);
    if (userElement) {
        userElement.remove();
    }
    if (userList.children.length === 1) { // Chỉ còn lại li "chưa có ai"
         if (noUsersMessage) noUsersMessage.style.display = 'block';
    }
     if (activeUserId === userId) {
        chatPanel.style.display = 'none';
        selectChatPrompt.style.display = 'block';
        activeUserId = null;
    }
}

// Hàm để hiển thị tin nhắn
function displayMessage(fromUser, message, isFromCurrentUser) {
     const li = document.createElement("li");
     li.style.listStyleType = "none";
     li.style.maxWidth = "80%";
     li.style.marginBottom = "10px";

     if (isFromCurrentUser) { // Admin gửi
         li.style.marginLeft = "auto";
         li.style.textAlign = "right";
         li.innerHTML = `<div style="background-color: #dcf8c6; color: #212529; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>Bạn:</strong> ${message}</div>`;
     } else { // User gửi
         li.style.marginRight = "auto";
         li.style.textAlign = "left";
         li.innerHTML = `<div style="background-color: #e9ecef; color: #212529; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>${fromUser}:</strong> ${message}</div>`;
     }
     adminChatMessages.appendChild(li);
     adminChatMessages.scrollTop = adminChatMessages.scrollHeight;
}


// Lắng nghe các sự kiện từ Hub
connection.on("UserConnected", (userId, username) => {
    addUserToList(userId, username);
});

connection.on("UserDisconnected", (userId) => {
    removeUserFromList(userId);
});

connection.on("ReceiveMessageFromUser", (senderUserId, senderName, message) => {
    if (activeUserId === senderUserId) {
        displayMessage(senderName, message, false);
    } else {
        // Hiển thị thông báo tin nhắn mới nếu không phải đang chat với user đó
        const userElement = document.getElementById(`user-${senderUserId}`);
        if (userElement) {
            const badge = userElement.querySelector('.badge');
            badge.style.display = 'inline-block';
            let count = parseInt(badge.textContent) || 0;
            badge.textContent = count + 1;
        } else {
            // Nếu user chưa có trong danh sách (kết nối lại) thì thêm vào
            addUserToList(senderUserId, senderName);
            const newUserElement = document.getElementById(`user-${senderUserId}`);
            if (newUserElement) {
                 const badge = newUserElement.querySelector('.badge');
                 badge.style.display = 'inline-block';
                 badge.textContent = '1';
            }
        }
    }
});

// Xử lý gửi tin nhắn từ Admin
adminChatForm.addEventListener('submit', (e) => {
    e.preventDefault();
    const targetUserId = targetUserIdInput.value;
    const message = adminMessageInput.value;
    if (targetUserId && message.trim()) {
        connection.invoke("SendMessageToUser", targetUserId, message).catch(err => console.error(err.toString()));
        displayMessage("Admin", message, true);
        adminMessageInput.value = '';
    }
});


// Bắt đầu kết nối
connection.start().catch(err => console.error(err.toString()));