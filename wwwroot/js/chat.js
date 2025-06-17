"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

const messageInput = document.getElementById("chat-message-input");
const chatForm = document.getElementById("chat-form");
const messagesList = document.getElementById("chat-messages-list");
const chatToggleButton = document.getElementById('chat-toggle-button');

const currentUserIdElement = document.getElementById('current-user-id');
const currentUserId = currentUserIdElement ? currentUserIdElement.value : null;

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

    let senderDisplay;
    let messageBgColor;
    let messageColor;

    // THÊM LOG ĐỂ KIỂM TRA:
    console.log("--- Display Message Debug Info ---");
    console.log("Sender ID:", senderId);
    console.log("Current User ID:", currentUserId);
    console.log("Is Admin Message:", isAdminMessage);
    console.log("----------------------------------");

    const isCurrentUserMessage = (senderId === currentUserId && !isAdminMessage);

    if (isCurrentUserMessage) {
        senderDisplay = "Bạn";
        messageBgColor = "#0d6efd";
        messageColor = "white";
        li.style.marginLeft = "auto";
        li.style.textAlign = "right";
    } else {
        senderDisplay = "Admin";
        messageBgColor = "#e9ecef";
        messageColor = "#212529";
        li.style.marginRight = "auto";
        li.style.textAlign = "left";
    }

    li.innerHTML = `<div style="background-color: ${messageBgColor}; color: ${messageColor}; border-radius: 10px; padding: 8px 12px; display: inline-block;"><strong>${senderDisplay}:</strong> ${message}<br><small style="font-size: 0.7em;">${formattedTime}</small></div>`;

    const defaultMsg = messagesList.querySelector(".text-center.text-muted");
    if (defaultMsg) {
        defaultMsg.remove();
    }

    messagesList.appendChild(li);
    messagesList.scrollTop = messagesList.scrollHeight;
}

if (chatToggleButton) {
    chatForm.querySelector("button").disabled = true;

    connection.on("ReceiveMessage", function (user, message, timestamp, senderId, isAdminMessage) {
        displayMessage(user, message, timestamp, senderId, isAdminMessage);
    });

    connection.on("ReceiveMessageFromUser", function (senderId, user, message, timestamp, isAdminMessage) {
        displayMessage(user, message, timestamp, senderId, isAdminMessage);
    });

    connection.start().then(function () {
        chatForm.querySelector("button").disabled = false;
    }).catch(function (err) {
        return console.error(err.toString());
    });

    chatForm.addEventListener("submit", function (event) {
        event.preventDefault(); 
        
        const message = messageInput.value;

        if (message.trim()) {
            connection.invoke("SendMessageToAdmin", message).catch(function (err) {
                return console.error(err.toString());
            });

            messageInput.value = "";
        }
    });
}