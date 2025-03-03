using System.ComponentModel.DataAnnotations;

namespace leo.ViewModels
{
    public class DashboardViewModel
    {
        public int CategoryCount { get; set; }
        public int ProductCount { get; set; }
        public int UsersCount { get; set; }
        public int SalesCount { get; set; }
        public int ReturnCount { get; set; }
        public int StockCount { get; set; } // StockCount property
        public int SupplierCount { get; set; } // SupplierCount property
        public decimal TotalSales { get; set; } // TotalSales property
        public decimal DailySales { get; set; }  // New property for daily sales
        public decimal WeeklySales { get; set; } // New property for weekly sales
        public decimal MonthlySales { get; set; } // New property for monthly sales
        public decimal AverageSalesPerTransaction { get; set; } // Average sales per transaction
        public List<int> MonthlyProductQuantities { get; set; } // For storing monthly quantities of products
        public List<decimal> MonthlyTotalSales { get; set; } // For storing monthly total sales
                                                             // Initializes ProductSalesData to prevent null reference issues
        public List<InventoryAnalyticsViewModel> InventoryAnalyticsData { get; set; } = new List<InventoryAnalyticsViewModel>();
        public List<LowStockItemViewModel> LowStockItems { get; set; } // Add this property
        public List<ProductSalesViewModel> ProductSalesData { get; set; } = new List<ProductSalesViewModel>();
        public List<int> MonthlyInventoryQuantities { get; internal set; }
        // Existing properties...
        public IEnumerable<ProductViewModel> LatestProducts { get; set; }
        public IEnumerable<ProductViewModel> TopSellingProducts { get; set; }
    }

    public class ProductSalesViewModel
    {
        public string ProductName { get; set; }
        public int QuantityOrdered { get; set; }
        public decimal TotalSales { get; set; } // Total sales for each product
    }
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Total Sold")]
        public int TotalSold { get; set; } // This will be used for top-selling products

        // Add any other properties that you might want to display
        // e.g. Product Image URL, Category, etc.
    }
}
