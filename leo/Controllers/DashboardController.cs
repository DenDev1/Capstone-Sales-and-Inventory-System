using leo.Data;
using leo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
            // Fetch counts and sums sequentially as DbContext is not thread-safe for parallel operations
            var categoryCount = await _context.Category.CountAsync();
            var productCount = await _context.Inventory.CountAsync();
            var usersCount = await _context.Users.CountAsync();
            var salesCount = await _context.Order.CountAsync();
            var supplierCount = await _context.Supplier.CountAsync();
            var totalSales = await _context.Order.SumAsync(o => o.TotalAmount);
            var stockCount = await _context.Inventory.SumAsync(p => p.StockQuantity);
            var returnCount = salesCount;

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

            // Optimize: Fetch all orders for current year in one go to memory
            var startOfYear = new DateTime(today.Year, 1, 1);
            var endOfYear = new DateTime(today.Year, 12, 31);
            var yearOrders = _context.Order
                .AsNoTracking()
                .Where(o => o.OrderDate >= startOfYear && o.OrderDate <= endOfYear)
                .ToList();

            var monthlyProductQuantities = new List<int>();
            var monthlyTotalSales = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var monthData = yearOrders.Where(o => o.OrderDate.Month == month);
                monthlyProductQuantities.Add(monthData.Sum(o => o.Quantity));
                monthlyTotalSales.Add(monthData.Sum(o => o.TotalAmount));
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

            // Optimize: Fetch all inventory records for current year once
            var yearInventory = _context.Inventory
                .AsNoTracking()
                .Where(p => p.Date >= startOfYear && p.Date <= endOfYear)
                .ToList();

            var monthlyInventoryQuantities = new List<int>();
            var inventoryAnalyticsData = new List<InventoryAnalyticsViewModel>();

            for (int month = 1; month <= 12; month++)
            {
                var totalProductQuantity = (int)yearInventory
                    .Where(p => p.Date.Month == month)
                    .Sum(p => p.StockQuantity);

                monthlyInventoryQuantities.Add(totalProductQuantity);
                inventoryAnalyticsData.Add(new InventoryAnalyticsViewModel
                {
                    Month = new DateTime(today.Year, month, 1).ToString("MMMM"),
                    TotalProductQuantity = totalProductQuantity
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
