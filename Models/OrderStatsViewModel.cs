namespace doanwebnangcao.Models
{
    public class OrderStatsViewModel
    {
        public string MonthYear { get; set; }
        public int TotalOrders { get; set; }
        public int PlacedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal PaidRevenue { get; set; }
        public decimal UnpaidRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}