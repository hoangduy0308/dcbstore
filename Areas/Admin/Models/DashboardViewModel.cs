namespace DCBStore.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // === CÁC THUỘC TÍNH HIỆN TẠI ===
        public decimal MonthlyRevenue { get; set; }
        public int NewOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int NewUsersThisMonth { get; set; }


        // === CÁC THUỘC TÍNH MỚI ĐƯỢC THÊM VÀO ĐỂ SO SÁNH ===

        /// <summary>
        /// Lưu trữ phần trăm thay đổi doanh thu so với tháng trước.
        /// Ví dụ: 15.5 (tăng 15.5%), -5.2 (giảm 5.2%)
        /// </summary>
        public decimal RevenueChangePercentage { get; set; }

        /// <summary>
        /// Lưu trữ phần trăm thay đổi tổng số đơn hàng so với tháng trước.
        /// </summary>
        public decimal OrdersChangePercentage { get; set; }

        /// <summary>
        /// Lưu trữ phần trăm thay đổi tổng số người dùng so với tháng trước.
        /// </summary>
        public decimal UsersChangePercentage { get; set; }
    }
}