namespace DCBStore.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public decimal MonthlyRevenue { get; set; }
        public int NewOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int NewUsersThisMonth { get; set; }
    }
}
