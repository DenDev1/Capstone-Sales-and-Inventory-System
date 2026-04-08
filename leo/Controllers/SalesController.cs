using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using leo.ViewModels;
using leo.Data;
using Microsoft.AspNetCore.Authorization;

namespace SOI_LEOTECH.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly leoContext _context;

        public SalesController(leoContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? startDate, DateTime? endDate, string searchQuery)
        {
            var startDateValue = (startDate ?? DateTime.Today.AddMonths(-1)).Date;
            var endDateValue = (endDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var ordersQuery = _context.Order!
                .AsNoTracking()
                .Include(o => o.Product)
                .Where(o => o.OrderDate >= startDateValue && o.OrderDate <= endDateValue);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var trimmedSearch = searchQuery.Trim();
                ordersQuery = ordersQuery.Where(o =>
                    o.Product != null &&
                    o.Product.ProductName.Contains(trimmedSearch));
            }

            var orders = ordersQuery.ToList();

            var dailySales = orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new DailySalesViewModel
                {
                    OrderDate = o.OrderDate,
                    ProductName = o.Product?.ProductName ?? "Unknown Product",
                    UnitPrice = o.UnitPrice,
                    Quantity = o.Quantity,
                    Subtotal = o.UnitPrice * o.Quantity,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentStatus.ToString()
                })
                .ToList();

            var monthlySales = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .Select(g => new MonthlySalesViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Calculate Top Products
            var topProducts = orders
                .GroupBy(o => o.Product?.ProductName ?? "Unknown Product")
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(o => o.Quantity),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            // Calculate Sales Trends (Daily)
            var salesTrends = orders
                .GroupBy(o => o.OrderDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SalesTrendViewModel
                {
                    DateLabel = g.Key.ToString("MMM dd"),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Calculate Payment Method Distribution
            var paymentMethods = orders
                .GroupBy(o => o.PaymentStatus)
                .Select(g => new PaymentMethodViewModel
                {
                    Method = g.Key.ToString(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    Count = g.Count()
                })
                .ToList();

            var viewModel = new SalesIndexViewModel
            {
                StartDate = startDateValue,
                EndDate = endDateValue.Date,
                DailySales = dailySales,
                MonthlySales = monthlySales,
                TopProducts = topProducts,
                SalesTrends = salesTrends,
                PaymentMethods = paymentMethods,
                SearchQuery = searchQuery ?? string.Empty,
                TotalProfit = dailySales.Sum(ds => ds.TotalAmount)
            };

            ViewBag.StartDate = startDateValue;
            ViewBag.EndDate = endDateValue.Date;
            return View(viewModel);
        }
    }
}
