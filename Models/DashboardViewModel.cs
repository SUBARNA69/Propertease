namespace Propertease.Models
{
    public class DashboardViewModel
    {
        public int Id { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveProperties { get; set; }
        public int PendingApprovals { get; set; }
        public int FlaggedProperties { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

}
