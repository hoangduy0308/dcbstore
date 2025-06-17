"use strict";

document.addEventListener('DOMContentLoaded', function () {

    // --- PHẦN 1: KIỂM TRA TRẠNG THÁI YÊU THÍCH KHI TẢI TRANG ---

    function checkWishlistStatus() {
        const wishlistButtons = document.querySelectorAll('.toggle-wishlist-btn');
        if (wishlistButtons.length === 0) return;

        // Lấy danh sách tất cả productId đang hiển thị trên trang
        const productIds = Array.from(wishlistButtons).map(btn => btn.dataset.productId);

        // Gọi API để lấy trạng thái
        fetch(`/Wishlist/GetWishlistStatus?productIds=${productIds.join(',')}`)
            .then(response => response.json())
            .then(likedProductIds => {
                // likedProductIds là một mảng các ID đã được yêu thích, ví dụ: [3, 10, 25]
                wishlistButtons.forEach(btn => {
                    const productId = btn.dataset.productId;
                    if (likedProductIds.includes(parseInt(productId))) {
                        btn.classList.add('liked');
                        btn.innerHTML = '<i class="fas fa-heart"></i>'; // Icon trái tim đầy
                    } else {
                         btn.classList.remove('liked');
                         btn.innerHTML = '<i class="far fa-heart"></i>'; // Icon trái tim rỗng
                    }
                });
            })
            .catch(error => console.error('Lỗi khi kiểm tra trạng thái wishlist:', error));
    }


    // --- PHẦN 2: XỬ LÝ SỰ KIỆN CLICK VÀO NÚT YÊU THÍCH ---

    function handleWishlistToggle(event) {
        // Dùng event delegation để chỉ cần 1 event listener
        const button = event.target.closest('.toggle-wishlist-btn');
        if (!button) return;

        event.preventDefault();
        const productId = button.dataset.productId;

        // Lấy anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        fetch('/Wishlist/ToggleWishlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `productId=${productId}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Cập nhật giao diện nút bấm
                if (data.added) {
                    button.classList.add('liked');
                    button.innerHTML = '<i class="fas fa-heart"></i>';
                } else {
                    button.classList.remove('liked');
                    button.innerHTML = '<i class="far fa-heart"></i>';
                }
                // (Tùy chọn) Hiển thị thông báo toast
                // showToast(data.message);
            } else {
                // Nếu lỗi do chưa đăng nhập, chuyển hướng đến trang đăng nhập
                if (data.message.includes("đăng nhập")) {
                    window.location.href = '/Identity/Account/Login';
                } else {
                    alert(data.message);
                }
            }
        })
        .catch(error => console.error('Lỗi khi toggle wishlist:', error));
    }
    
    // Gán event listener cho toàn bộ document
    document.addEventListener('click', handleWishlistToggle);

    // Chạy hàm kiểm tra trạng thái khi trang được tải xong
    checkWishlistStatus();
});

// Thêm form chứa AntiForgeryToken vào cuối trang nếu nó chưa tồn tại
if (!document.getElementById('anti-forgery-token-wishlist-form')) {
    const form = document.createElement('form');
    form.id = 'anti-forgery-token-wishlist-form';
    form.innerHTML = '@Html.AntiForgeryToken()'; // Dòng này sẽ được Razor render đúng cách
    document.body.appendChild(form);
}