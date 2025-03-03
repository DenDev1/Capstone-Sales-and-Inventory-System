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
    public class DashboardController : Controller
    {
        private readonly leoContext _context;

        public DashboardController(leoContext context)
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
            var stockCount = _context.Inventory.Sum(p => p.StockQuantity);
            var supplierCount = _context.Supplier.Count();

            var totalSales = _context.Order.Sum(o => o.TotalAmount);

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

            // Fetch Latest Products (top 1 recent products)
            var latestProducts = _context.Inventory
                .OrderByDescending(p => p.Date) // assuming Date represents creation or updated date
                .Take(1)
                .Select(p => new ProductViewModel
                {
                    Id = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.UnitPrice,
                    StockQuantity = p.StockQuantity
                })
                .ToList();

            // Fetch Top Selling Products (top 1 based on total sales quantity)
            var topSellingProducts = _context.Order
                .GroupBy(o => o.Product.ProductId)
                .OrderByDescending(g => g.Sum(o => o.Quantity))
                .Take(1)
                .Select(g => new ProductViewModel
                {
                    Id = g.Key,
                    ProductName = g.FirstOrDefault().Product.ProductName,
                    Price = g.FirstOrDefault().Product.UnitPrice,
                    StockQuantity = g.FirstOrDefault().Product.StockQuantity,
                    TotalSold = g.Sum(o => o.Quantity)
                })
                .ToList();

            // Calculate Monthly Product Quantities and Total Sales
            var monthlyProductQuantities = new List<int>();
            var monthlyTotalSales = new List<decimal>();

            for (int month = 0; month < 12; month++)
            {
                var firstDayOfMonth = new DateTime(today.Year, month + 1, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var quantity = _context.Order
                    .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderDate <= lastDayOfMonth)
                    .Sum(o => o.Quantity);

                monthlyProductQuantities.Add(quantity);

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
                    TotalSales = g.Sum(o => o.TotalAmount)
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

            var monthlyInventoryQuantities = new List<int>();
            var inventoryAnalyticsData = new List<InventoryAnalyticsViewModel>();

            for (int month = 0; month < 12; month++)
            {
                var firstDayOfMonth = new DateTime(today.Year, month + 1, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var totalProductQuantity = _context.Inventory
                    .Where(p => p.Date >= firstDayOfMonth && p.Date <= lastDayOfMonth)
                    .Sum(p => p.StockQuantity);

                monthlyInventoryQuantities.Add((int)totalProductQuantity);

                inventoryAnalyticsData.Add(new InventoryAnalyticsViewModel
                {
                    Month = firstDayOfMonth.ToString("MMMM"),
                    TotalProductQuantity = (int)totalProductQuantity
                });
            }

            var viewModel = new DashboardViewModel
            {
                LowStockItems = lowStockItems,
                CategoryCount = categoryCount,
                ProductCount = productCount,
                ReturnCount = returnCount,
                UsersCount = usersCount,
                SalesCount = salesCount,
                StockCount = (int)stockCount,
                SupplierCount = supplierCount,
                TotalSales = totalSales,
                DailySales = dailySales,
                WeeklySales = weeklySales,
                MonthlySales = monthlySales,
                MonthlyProductQuantities = monthlyProductQuantities,
                MonthlyTotalSales = monthlyTotalSales,
                ProductSalesData = productSalesData.Select(p => new ProductSalesViewModel
                {
                    ProductName = p.ProductName,
                    QuantityOrdered = p.QuantityOrdered,
                    TotalSales = p.TotalSales
                }).ToList(),
                MonthlyInventoryQuantities = monthlyInventoryQuantities,
                InventoryAnalyticsData = inventoryAnalyticsData,
                LatestProducts = latestProducts,
                TopSellingProducts = topSellingProducts
            };

            return View(viewModel);
        }

    }
}
