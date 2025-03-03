using leo.Models;

namespace leo.ViewModels
{
    public class InventoryReportViewModel
    {
        //// List of inventory items sold within the last week
        //public List<Inventory> WeeklySalesItems { get; set; }

        // List of inventory items sold within the last month
        public List<Inventory> MonthlySalesItems { get; set; }

        // StockLevel - Based on StockQuantity
        public int TotalSoldWeekly { get; set; }
        public int TotalSoldMonthly { get; set; }
        // The username of the person preparing the report
        public string PreparedBy { get; set; }
        public int StockAdded { get; set; } // Include StockIn here
    }

}
