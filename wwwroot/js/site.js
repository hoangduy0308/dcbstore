// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/**
 * Hiển thị một thông báo "toast" trượt vào từ góc màn hình.
 * @param {string} message Nội dung của thông báo.
 * @param {string} type Loại thông báo ('success' hoặc 'error').
 */
function showNotification(message, type = 'success') {
    const container = document.querySelector('.toast-container');
    if (!container) {
        console.error('Toast container not found!');
        return;
    }

    const toastId = 'toast-' + Date.now();
    const bgColor = type === 'success' ? 'bg-success' : 'bg-danger';
    const iconHtml = type === 'success' 
        ? '<i class="fas fa-check-circle me-2"></i>' 
        : '<i class="fas fa-times-circle me-2"></i>';

    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgColor}" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${iconHtml}
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    // Thêm toast vào container
    container.insertAdjacentHTML('beforeend', toastHtml);

    // Khởi tạo và hiển thị toast bằng Bootstrap
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        delay: 5000 // Tự động ẩn sau 5 giây
    });
    
    toast.show();

    // Xóa toast khỏi DOM sau khi nó đã ẩn
    toastElement.addEventListener('hidden.bs.toast', function () {
        toastElement.remove();
    });
}