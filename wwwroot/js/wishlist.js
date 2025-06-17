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
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => { // Đổi tên biến từ 'likedProductIds' thành 'data' để rõ ràng hơn
                let actualLikedProductIds;
                // Kiểm tra nếu phản hồi có dạng {"$id":"1","$values":[...]}
                if (data && data.$values && Array.isArray(data.$values)) {
                    actualLikedProductIds = data.$values;
                } else if (Array.isArray(data)) {
                    // Nếu phản hồi trực tiếp là một mảng
                    actualLikedProductIds = data;
                } else {
                    // Trường hợp không mong muốn, gán là mảng rỗng
                    actualLikedProductIds = [];
                    console.warn("Định dạng phản hồi API GetWishlistStatus không như mong đợi:", data);
                }

                wishlistButtons.forEach(btn => {
                    const productId = btn.dataset.productId;
                    if (actualLikedProductIds.includes(parseInt(productId))) {
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
        const button = event.target.closest('.toggle-wishlist-btn');
        if (!button) return;

        event.preventDefault();
        const productId = button.dataset.productId;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        fetch('/Wishlist/ToggleWishlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `productId=${productId}&__RequestVerificationToken=${token}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (data.added) {
                    button.classList.add('liked');
                    button.innerHTML = '<i class="fas fa-heart"></i>';
                } else {
                    button.classList.remove('liked');
                    button.innerHTML = '<i class="far fa-heart"></i>';
                }
            } else {
                if (data.message.includes("đăng nhập")) {
                    window.location.href = '/Identity/Account/Login';
                } else {
                    alert(data.message);
                }
            }
        })
        .catch(error => console.error('Lỗi khi toggle wishlist:', error));
    }
    
    document.addEventListener('click', handleWishlistToggle);

    checkWishlistStatus();
});

if (!document.getElementById('anti-forgery-token-wishlist-form')) {
    const form = document.createElement('form');
    form.id = 'anti-forgery-token-wishlist-form';
    form.style.display = 'none';
    document.body.appendChild(form);
}