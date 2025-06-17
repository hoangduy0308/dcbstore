/* Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
for details on configuring this project to bundle and minify static web assets. */

/* Write your JavaScript code. */

/* Bắt đầu thêm mới CSS cho bảng trong Admin */
.table-responsive .table td {
    white-space: nowrap; /* Ngăn không cho văn bản xuống dòng */
    overflow: hidden; /* Ẩn phần văn bản tràn ra ngoài */
    text-overflow: ellipsis; /* Hiển thị dấu ba chấm cho phần văn bản bị ẩn */
}

/* Có thể tùy chỉnh độ rộng tối đa cho cột tên sản phẩm nếu cần */
.table-responsive .table tbody tr td:first-child {
    max-width: 250px; /* Điều chỉnh giá trị này nếu tên sản phẩm quá dài */
}
/* Kết thúc thêm mới CSS */