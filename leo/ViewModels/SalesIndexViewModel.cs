using System;
using System.Collections.Generic;

namespace leo.ViewModels
{
    public class SalesIndexViewModel
    {
        public string SearchQuery { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<DailySalesViewModel> DailySales { get; set; }
        public List<MonthlySalesViewModel> MonthlySales { get; set; }
        public decimal? TotalProfit { get; set; }

        // New analytical properties
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
        public List<SalesTrendViewModel> SalesTrends { get; set; } = new List<SalesTrendViewModel>();
        public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new List<PaymentMethodViewModel>();
        public decimal AverageOrderValue => DailySales.Count > 0 ? (TotalProfit ?? 0) / DailySales.Count : 0;
        public int TotalTransactions => DailySales.Count;

        public void ResetFilters()
        {
            SearchQuery = string.Empty;
            StartDate = null;
            EndDate = null;
        }
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesTrendViewModel
    {
        public string DateLabel { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentMethodViewModel
    {
        public string Method { get; set; }
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }
}
