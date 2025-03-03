//using Microsoft.AspNetCore.Mvc;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic; // Ensure you have this for List<T>
//using leo.Models;
//using leo.ViewModels;
//using leo.Data;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.EntityFrameworkCore;

//namespace leo.Controllers
//{
//    [Authorize]
//    public class SalesReportController : Controller
//    {
//        private readonly leoContext _context;

//        public SalesReportController(leoContext context)
//        {
//            _context = context;
//        }

//        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
//        {
//            // Set default date range if none is provided (e.g., last month)
//            var startDateValue = startDate ?? DateTime.Now.AddMonths(-1);
//            var endDateValue = endDate ?? DateTime.Now;

//            // Fetch orders within the specified date range
//            var orders = await _context.Order
//                .Where(o => o.OrderDate >= startDateValue && o.OrderDate <= endDateValue)
//                .ToListAsync();

//            // Handle cases where orders might be null or empty
//            if (orders == null || !orders.Any())
//            {
//                // Return an empty view model if no orders are found
//                return View(new SalesReportViewModel
//                {
//                    TotalProfit = 0,
//                    AverageSalesPerTransaction = 0,
//                    MonthlyRevenueData = new List<MonthlyRevenueViewModel>() // Initialize as an empty list
//                });
//            }

//            // Calculate total profit and average sales per transaction
//            var totalProfit = orders.Sum(o => o?.TotalAmount ?? 0); // Use null coalescing to handle null TotalAmount
//            var totalTransactions = orders.Count();
//            var averageSalesPerTransaction = totalTransactions > 0 ? totalProfit / totalTransactions : 0;

//            var viewModel = new SalesReportViewModel
//            {
//                TotalProfit = totalProfit,
//                AverageSalesPerTransaction = averageSalesPerTransaction,
//                MonthlyRevenueData = CalculateMonthlyRevenue(orders) // Implement this method to calculate monthly revenue
//            };

//            // Pass the start and end date to the view for display purposes
//            ViewBag.StartDate = startDateValue;
//            ViewBag.EndDate = endDateValue;

//            return View(viewModel);
//        }

//        private List<MonthlyRevenueViewModel> CalculateMonthlyRevenue(IEnumerable<Order> orders)
//        {
//            return orders
//                .Where(o => o?.OrderDate != null) // Ensure OrderDate is not null
//                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
//                .Select(g => new MonthlyRevenueViewModel
//                {
//                    Month = g.Key.Month, // Use Month as int
//                    Year = g.Key.Year,   // You may want to add a Year property to your MonthlyRevenueViewModel
//                    TotalRevenue = g.Sum(o => o?.TotalAmount ?? 0) // Handle null TotalAmount
//                })
//                .ToList();
//        }

//    }
//}
