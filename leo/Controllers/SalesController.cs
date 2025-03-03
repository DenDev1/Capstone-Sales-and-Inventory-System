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
            // Set default date range if none is provided (e.g., last month)
            var startDateValue = startDate ?? DateTime.Now.AddMonths(-1);
            var endDateValue = endDate ?? DateTime.Now;


            var ordersQuery = _context.Order
                .Include(o => o.Product) // Make sure to include Product data
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                ordersQuery = ordersQuery.Where(o => o.Product.ProductName.Contains(searchQuery)); // Assuming Product has a Name property
            }

            var orders = ordersQuery.ToList();

            var dailySales = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailySalesViewModel
                {
                    OrderDate = g.Key,
                    ProductName = g.First().Product.ProductName, // Assuming Product has a Name property
                    UnitPrice = g.First().UnitPrice,
                    Quantity = g.First().Quantity,
                    Subtotal = g.Sum(o => o.UnitPrice * o.Quantity),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    PaymentMethod = g.First().PaymentStatus.ToString() // Adjust based on actual enum
                })
                .ToList();

            var monthlySales = orders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlySalesViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            var viewModel = new SalesIndexViewModel
            {
                StartDate = startDate, // Use the nullable type directly
                EndDate = endDate,     // Use the nullable type directly
                DailySales = dailySales,
                MonthlySales = monthlySales,
                SearchQuery = searchQuery,
                TotalProfit = dailySales.Sum(ds => ds.TotalAmount) // Example calculation for TotalProfit
            };
            // Pass the start and end date to the view for display purposes
            ViewBag.StartDate = startDateValue;
            ViewBag.EndDate = endDateValue;
            return View(viewModel);
        }
    }
}
