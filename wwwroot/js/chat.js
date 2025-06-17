"use strict";

// Tạo kết nối đến ChatHub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

// Lấy các element trên trang
const messageInput = document.getElementById("chat-message-input");
const chatForm = document.getElementById("chat-form");
const messagesList = document.getElementById("chat-messages-list");
const chatToggleButton = document.getElementById('chat-toggle-button');

// Hàm để hiển thị một tin nhắn lên giao diện
function displayMessage(user, message, isCurrentUser) {
    const li = document.createElement("li");
    li.style.listStyleType = "none";
    li.style.maxWidth = "80%";
    li.style.marginBottom = "10px";

    if (isCurrentUser) {
        li.style.marginLeft = "auto"; // Tin nhắn của mình nằm bên phải
        li.style.textAlign = "right";
        li.innerHTML = `<div style="background-color: #0d6efd; color: white; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>Bạn:</strong> ${message}</div>`;
    } else {
        li.style.marginRight = "auto"; // Tin nhắn của người khác nằm bên trái
        li.style.textAlign = "left";
        li.innerHTML = `<div style="background-color: #e9ecef; color: #212529; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>${user}:</strong> ${message}</div>`;
    }

    const defaultMsg = messagesList.querySelector(".text-center.text-muted");
    if (defaultMsg) {
        defaultMsg.remove();
    }

    messagesList.appendChild(li);
    messagesList.scrollTop = messagesList.scrollHeight;
}


if (chatToggleButton) {
    chatForm.querySelector("button").disabled = true;

    // Lắng nghe sự kiện "ReceiveMessage" từ Hub (khi Admin gửi tin nhắn)
    connection.on("ReceiveMessage", function (user, message) {
        // Admin gửi tin nhắn, nên isCurrentUser là false
        displayMessage(user, message, false);
    });

    // Bắt đầu kết nối
    connection.start().then(function () {
        chatForm.querySelector("button").disabled = false;
    }).catch(function (err) {
        return console.error(err.toString());
    });

    // Xử lý sự kiện khi người dùng gửi form
    chatForm.addEventListener("submit", function (event) {
        event.preventDefault(); 
        
        const message = messageInput.value;
        const currentUsername = document.getElementById('current-username')?.innerText.replace('Xin chào, ', '').trim() || "User";

        if (message.trim()) {
            // ====> THAY ĐỔI QUAN TRỌNG <====
            // Gọi đúng tên phương thức "SendMessageToAdmin" và chỉ gửi 1 tham số là "message"
            connection.invoke("SendMessageToAdmin", message).catch(function (err) {
                return console.error(err.toString());
            });

            // Hiển thị ngay tin nhắn của người dùng trên màn hình của họ mà không cần chờ server
            displayMessage(currentUsername, message, true);

            // Xóa nội dung trong ô input sau khi gửi
            messageInput.value = "";
        }
    });
}