using System;
using System.Collections.Generic;

namespace leo.ViewModels
{
    public class SalesIndexViewModel
    {
        public string SearchQuery { get; set; } // For search functionality
        public DateTime? StartDate { get; set; } // Start date for filtering
        public DateTime? EndDate { get; set; } // End date for filtering
        public List<DailySalesViewModel> DailySales { get; set; }
        public List<MonthlySalesViewModel> MonthlySales { get; set; }
        public decimal? TotalProfit { get; set; } // Add this property

        public void ResetFilters()
        {
            SearchQuery = string.Empty;
            StartDate = null;
            EndDate = null;
        }
    }

}

