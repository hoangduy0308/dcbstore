"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

const userList = document.getElementById("user-list");
const chatPanel = document.getElementById("chat-panel");
const selectChatPrompt = document.getElementById("select-chat-prompt");
const adminChatMessages = document.getElementById("admin-chat-messages");
const adminChatForm = document.getElementById("admin-chat-form");
const adminMessageInput = document.getElementById("admin-message-input");
const chatWithUsername = document.getElementById("chat-with-username");
const targetUserIdInput = document.getElementById("target-user-id");
const noUsersMessage = document.getElementById("no-users-message");

const currentAdminIdElement = document.getElementById('current-admin-id');
const currentAdminId = currentAdminIdElement ? currentAdminIdElement.value : null;

let activeUserId = null;

function formatTimestamp(timestamp) {
    const date = new Date(timestamp);
    const options = { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false };
    return date.toLocaleTimeString('vi-VN', options) + ' ' + date.toLocaleDateString('vi-VN');
}

function displayMessage(user, message, timestamp, senderId, isAdminMessage) {
    const li = document.createElement("li");
    li.style.listStyleType = "none";
    li.style.maxWidth = "80%";
    li.style.marginBottom = "10px";

    const formattedTime = formatTimestamp(timestamp);

    let senderDisplay = user;
    let messageBgColor = "#e9ecef";
    let messageColor = "#212529";

    if (isAdminMessage) {
        if (senderId === currentAdminId) {
            senderDisplay = "Bạn (Admin)";
            messageBgColor = "#dcf8c6";
            messageColor = "#212529";
            li.style.marginLeft = "auto";
            li.style.textAlign = "right";
        } else {
            senderDisplay = `Admin (${user})`;
            li.style.marginRight = "auto";
            li.style.textAlign = "left";
        }
    } else {
        senderDisplay = user;
        li.style.marginRight = "auto";
        li.style.textAlign = "left";
    }

    li.innerHTML = `<div style="background-color: ${messageBgColor}; color: ${messageColor}; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>${senderDisplay}:</strong> ${message}<br><small style="font-size: 0.7em;">${formattedTime}</small></div>`;

    adminChatMessages.appendChild(li);
    adminChatMessages.scrollTop = adminChatMessages.scrollHeight;
}

function addUserToList(userId, username) {
    if (noUsersMessage) noUsersMessage.style.display = 'none';

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

        const currentActive = document.querySelector('#user-list .list-group-item.active');
        if(currentActive) currentActive.classList.remove('active');

        li.classList.add('active');
        li.querySelector('.badge').style.display = 'none';

        chatPanel.style.display = 'block';
        selectChatPrompt.style.display = 'none';
        
        chatWithUsername.textContent = username;
        targetUserIdInput.value = userId;
        activeUserId = userId;
        adminChatMessages.innerHTML = '';

        connection.invoke("LoadSpecificUserChatHistory", activeUserId).catch(err => console.error(err.toString()));
    });
}

function removeUserFromList(userId) {
    const userElement = document.getElementById(`user-${userId}`);
    if (userElement) {
        userElement.remove();
    }
    if (userList.children.length === 0) {
        if (noUsersMessage) noUsersMessage.style.display = 'block';
    }
    if (activeUserId === userId) {
        chatPanel.style.display = 'none';
        selectChatPrompt.style.display = 'block';
        activeUserId = null;
    }
}

connection.on("UserConnected", (userId, username) => {
    addUserToList(userId, username);
});

connection.on("UserDisconnected", (userId) => {
    removeUserFromList(userId);
});

// SỬA ĐỔI QUAN TRỌNG: Cho phép tin nhắn của chính admin hiển thị mà không thêm vào danh sách user
connection.on("ReceiveMessageFromUser", (senderUserId, senderName, message, timestamp, isAdminMessage) => {
    // Nếu tin nhắn là từ chính admin hiện tại đang xem
    if (isAdminMessage && senderUserId === currentAdminId) {
        // Và cuộc trò chuyện với người dùng đang hoạt động
        if (activeUserId) {
            displayMessage(senderName, message, timestamp, senderUserId, isAdminMessage);
        }
        return; // Không thêm admin vào danh sách user và không xử lý tiếp cho các tin nhắn admin khác
    }

    // Nếu tin nhắn đến từ một admin khác (không phải admin hiện tại)
    if (isAdminMessage) {
        return; // Bỏ qua, không hiển thị tin nhắn của admin khác trong khung chat user-specific này
    }

    // Nếu là tin nhắn từ người dùng thông thường
    if (activeUserId === senderUserId) {
        displayMessage(senderName, message, timestamp, senderUserId, isAdminMessage);
    } else {
        const userElement = document.getElementById(`user-${senderUserId}`);
        if (userElement) {
            const badge = userElement.querySelector('.badge');
            badge.style.display = 'inline-block';
            let count = parseInt(badge.textContent) || 0;
            badge.textContent = count + 1;
        } else {
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

connection.on("ReceiveMessage", function (user, message, timestamp, senderId, isAdminMessage) {
    if (senderId === currentAdminId || activeUserId) {
         displayMessage(user, message, timestamp, senderId, isAdminMessage);
    }
});

adminChatForm.addEventListener('submit', (e) => {
    e.preventDefault();
    const targetUserId = targetUserIdInput.value;
    const message = adminMessageInput.value;
    if (targetUserId && message.trim()) {
        connection.invoke("SendMessageToUser", targetUserId, message).catch(err => console.error(err.toString()));
        adminMessageInput.value = '';
    }
});

connection.start().catch(err => console.error(err.toString()));