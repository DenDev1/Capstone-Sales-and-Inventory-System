using leo.Data;
using leo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOI_LEOTECH.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly leoContext _context;

        public HomeController(leoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categoryCount = _context.Category.Count();
            var productCount = _context.Inventory.Count();
            var usersCount = _context.Users.Count();
            var salesCount = _context.Order.Count();
            var returnCount = _context.Order.Count();
            var stockCount = _context.Order.Count();
            var supplierCount = _context.Order.Count();

            // Calculate total sales
            var totalSales = _context.Order.Sum(o => o.TotalAmount); // Assuming you have a TotalAmount property in your Order model

            // Calculate daily, weekly, and monthly sales
            var today = DateTime.Today;
            var dailySales = _context.Order
                .Where(o => o.OrderDate >= today)
                .Sum(o => o.TotalAmount);

            var weeklySales = _context.Order
                .Where(o => o.OrderDate >= today.AddDays(-7))
                .Sum(o => o.TotalAmount);

            var monthlySales = _context.Order
                .Where(o => o.OrderDate >= today.AddMonths(-1))
                .Sum(o => o.TotalAmount);

            // Calculate Monthly Product Quantities and Total Sales
            var monthlyProductQuantities = new List<int>();
            var monthlyTotalSales = new List<decimal>();

            for (int month = 0; month < 12; month++)
            {
                var firstDayOfMonth = new DateTime(today.Year, month + 1, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Quantity ordered for the month
                var quantity = _context.Order
                    .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate <= lastDayOfMonth)
                    .Sum(o => o.Quantity);

                monthlyProductQuantities.Add(quantity);

                // Total sales for the month
                var totalSalesForMonth = _context.Order
                    .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate <= lastDayOfMonth)
                    .Sum(o => o.TotalAmount);

                monthlyTotalSales.Add(totalSalesForMonth);
            }

            var productSalesData = _context.Order
                .GroupBy(o => o.Product.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    QuantityOrdered = g.Sum(o => o.Quantity),
                    TotalSales = g.Sum(o => o.TotalAmount) // Total sales for each product
                })
                .ToList();


            var lowStockItems = _context.Inventory
                .Where(p => p.StockQuantity < 5)
                .Select(p => new LowStockItemViewModel
                {
                    ProductName = p.ProductName,
                    Quantity = (int)p.StockQuantity
                })
                .ToList();


            var viewModel = new DashboardViewModel
            {

                LowStockItems = lowStockItems,// Assign low stock items
                CategoryCount = categoryCount,
                ProductCount = productCount,
                ReturnCount = returnCount,
                UsersCount = usersCount,
                SalesCount = salesCount,
                StockCount = stockCount,
                SupplierCount = supplierCount,
                TotalSales = totalSales, // Set total sales
                DailySales = dailySales,  // Set daily sales
                WeeklySales = weeklySales, // Set weekly sales
                MonthlySales = monthlySales, // Set monthly sales
                MonthlyProductQuantities = monthlyProductQuantities, // Add this line
                MonthlyTotalSales = monthlyTotalSales, // Add this line
                ProductSalesData = productSalesData.Select(p => new ProductSalesViewModel
                {
                    ProductName = p.ProductName,
                    QuantityOrdered = p.QuantityOrdered,
                    TotalSales = p.TotalSales // Total sales for each product
                }).ToList(),



            };

            return View(viewModel);




        }
    }
}
