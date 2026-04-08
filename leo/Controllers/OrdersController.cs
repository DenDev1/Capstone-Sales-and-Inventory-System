using leo.Data;
using leo.Models;
using leo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace leo.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService; // Injecting AuditLogService

        public OrderController(leoContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        //public IActionResult Index()
        //{
        //    var orders = _context.Order
        //        .Include(o => o.Product) // Include the related Product entity
        //        .ToList(); // Fetch orders from the database
        //    return View(orders);
        //}


        public IActionResult Create()
        {
            var order = new Order(); // Initialize the model
            
            var categories = _context.Category
                .OrderBy(c => c.CategoryName)
                .ToList();

            object posProducts = _context.Inventory == null
                ? Array.Empty<object>()
                : _context.Inventory
                    .Include(p => p.Category)
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.UnitPrice,
                        p.StockQuantity,
                        p.Barcode,
                        p.ImagePath,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Uncategorized"
                    })
                    .ToList();

            // Get the enum values for PaymentStatus and exclude FullyPaid
            var paymentStatuses = Enum.GetValues(typeof(PaymentStatus)) 
                .Cast<PaymentStatus>()
                .Where(ps => ps != PaymentStatus.FullyPaid) // Exclude FullyPaid
                .Select(ps => new SelectListItem
                {
                    Value = ps.ToString(),
                    Text = ps.ToString()
                })
                .ToList();

            ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName");
            ViewBag.PaymentStatus = new SelectList(paymentStatuses, "Value", "Text"); // Pass the filtered list to the ViewBag
            ViewBag.PosProducts = posProducts;
            ViewBag.Categories = categories;

            return View(order); // Pass the initialized model to the view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Invalid checkout request." });
            }

            request.CustomerName = (request.CustomerName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return BadRequest(new { success = false, message = "Customer name is required." });
            }

            if (!Regex.IsMatch(request.CustomerName, @"^[a-zA-Z\s]+$"))
            {
                return BadRequest(new { success = false, message = "Customer name must contain only letters and spaces." });
            }

            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(new { success = false, message = "Please select at least one item." });
            }

            var normalizedPayment = string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)
                ? PaymentStatus.Cash
                : PaymentStatus.FullyPaid;
            var paymentLabel = normalizedPayment == PaymentStatus.Cash ? "Cash" : "Online";

            var productIds = request.Items
                .Select(item => item.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.Inventory!
                .Where(p => productIds.Contains(p.ProductId) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.ProductId);

            foreach (var item in request.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    return BadRequest(new { success = false, message = $"Product with ID {item.ProductId} was not found." });
                }

                if (item.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = $"Invalid quantity for {product.ProductName}." });
                }

                if (item.Quantity > product.StockQuantity)
                {
                    return BadRequest(new { success = false, message = $"Insufficient stock for {product.ProductName}." });
                }
            }

            var now = DateTime.Now;
            var createdOrders = new List<Order>();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in request.Items)
                {
                    var product = products[item.ProductId];
                    var lineTotal = product.UnitPrice * item.Quantity;

                    var order = new Order
                    {
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        PaymentStatus = normalizedPayment,
                        OrderDate = now,
                        ReferenceNo = GenerateReferenceNo(),
                        CustomerName = request.CustomerName,
                        PartialPaymentAmount = normalizedPayment == PaymentStatus.Partial ? lineTotal : 0m,
                        Barcode = product.Barcode ?? string.Empty,
                        UnitPrice = product.UnitPrice,
                        TotalAmount = lineTotal
                    };

                    createdOrders.Add(order);
                    _context.Order!.Add(order);

                    product.StockQuantity -= item.Quantity;
                    ApplyStockStatus(product);
                    _context.Inventory!.Update(product);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var subtotal = createdOrders.Sum(o => o.TotalAmount);
                var total = subtotal;
                var response = new
                {
                    success = true,
                    message = "Payment completed successfully.",
                    orderIds = createdOrders.Select(o => o.OrderId).ToList(),
                    invoice = new
                    {
                        customerName = request.CustomerName,
                        paymentMethod = paymentLabel,
                        timestamp = now,
                        subtotal,
                        total,
                        items = createdOrders.Select(order => new
                        {
                            productId = order.ProductId,
                            productName = products[order.ProductId].ProductName,
                            quantity = order.Quantity,
                            unitPrice = order.UnitPrice,
                            totalAmount = order.TotalAmount
                        }).ToList()
                    }
                };

                try
                {
                    await _auditLogService.LogActionAsync(
                        "POS Checkout",
                        $"Customer: {request.CustomerName}, Items: {createdOrders.Count}, Total: {createdOrders.Sum(o => o.TotalAmount):0.00}");
                }
                catch
                {
                    // Do not fail the cashier flow when audit logging has an issue.
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorDetails = ex.InnerException != null
                    ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                    : ex.Message;

                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to complete payment right now.",
                    details = errorDetails
                });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,ProductId,Quantity,PaymentStatus,OrderDate,CustomerName,PartialPaymentAmount,Barcode")] Order order)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Inventory.FindAsync(order.ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                // Check if the requested quantity is available
                if (order.Quantity > product.StockQuantity)
                {
                    ViewData["ErrorMessage"] = "Insufficient stock quantity."; // Add this line
                    ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName", order.ProductId);
                    ViewBag.PaymentStatus = new SelectList(Enum.GetValues(typeof(PaymentStatus)), order.PaymentStatus);
                    return View(order);
                }

                // Set OrderDate to current date if not provided
                if (order.OrderDate == default)
                {
                    order.OrderDate = DateTime.Now;
                }

                // Calculate unit price
                order.UnitPrice = product.UnitPrice;
                order.Barcode = product.Barcode;
                order.ReferenceNo = string.IsNullOrWhiteSpace(order.ReferenceNo) ? GenerateReferenceNo() : order.ReferenceNo;

                // Calculate subtotal based on quantity and unit price
                var subtotal = order.Quantity * order.UnitPrice;

                order.TotalAmount = subtotal; // If total amount is just the subtotal, you can keep it the same

                // Save the order
                _context.Add(order);
                await _context.SaveChangesAsync();

                // Update product stock quantity
                product.StockQuantity -= order.Quantity;
                ApplyStockStatus(product);
                _context.Update(product);
                await _context.SaveChangesAsync();

                // Log the order creation action
                await _auditLogService.LogActionAsync("Order", $"Product: {product.ProductName}, Quantity: {order.Quantity}");


                // Set TempData for login success
                TempData["LoginSuccess"] = "Added  successfully";


                return RedirectToAction(nameof(Create), new { id = order.OrderId });
            }

            ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName", order.ProductId);
            ViewBag.PaymentStatus = new SelectList(Enum.GetValues(typeof(PaymentStatus)), order.PaymentStatus);
            return View(order);
        }

        private static string GenerateReferenceNo()
        {
            return $"REF-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }

        private static void ApplyStockStatus(Inventory product)
        {
            if (product.StockQuantity <= 0)
            {
                product.StockStatus = StockStatus.OutOfStock;
            }
            else if (product.StockQuantity <= 5)
            {
                product.StockStatus = StockStatus.LowStock;
            }
            else
            {
                product.StockStatus = StockStatus.InStock;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return Json(null);
            }

            var product = await _context.Inventory.FirstOrDefaultAsync(p => p.Barcode == barcode);
            if (product == null)
            {
                return Json(null);
            }

            return Json(new
            {
                productId = product.ProductId,
                productName = product.ProductName,
                unitPrice = product.UnitPrice,
                stockQuantity = product.StockQuantity,
                stockStatus = product.StockStatus.ToString(),
                barcode = product.Barcode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductPrice(int id)
        {
            var product = await _context.Inventory.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Json(product.UnitPrice);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentOrders(bool forPrint = false)
        {
            var query = _context.Order
                .Include(o => o.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(10); // Get last 10 orders

            if (forPrint)
            {
                var orders = await query.ToListAsync();
                return View("PrintRecentOrders", orders);
            }

            var recentOrders = await query
                .Select(o => new
                {
                    orderId = o.OrderId,
                    productId = o.ProductId,
                    productName = o.Product.ProductName,
                    barcode = o.Barcode ?? o.Product.Barcode,
                    quantity = o.Quantity,
                    unitPrice = o.UnitPrice,
                    totalAmount = o.TotalAmount,
                    customerName = o.CustomerName,
                    orderDate = o.OrderDate,
                    paymentStatus = o.PaymentStatus.ToString()
                })
                .ToListAsync();

            return Json(recentOrders);
        }
        public async Task<IActionResult> Details(int id)
        {
            // For multiple products per order, you'll need to adjust your data model
            // to have OrderItems collection in your Order class

            var orders = await _context.Order
                .Include(o => o.Product)
                .Where(o => o.OrderId == id) // Or whatever condition groups your orders
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return NotFound();
            }

            var viewModel = new OrderDetailsViewModel
            {
                Orders = orders,
                TotalAmount = orders.Sum(o => o.Quantity * o.UnitPrice)
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            // Remove all orders from the database
            var allOrders = _context.Order.ToList();

            _context.Order.RemoveRange(allOrders);
            await _context.SaveChangesAsync();

       

            // After clearing the records, redirect to a page (like the list or index)
            return RedirectToAction("Create"); // Or redirect to any other page you want
        }




        public async Task<IActionResult> PrintAllInvoices()
        {
            // Get all orders that need to be printed (you might want to filter by date or status)
            var orders = await _context.Order
                .Include(o => o.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return NotFound();
            }

            var viewModel = new OrderDetailsViewModel
            {
                Orders = orders,
                TotalAmount = orders.Sum(o => o.Quantity * o.UnitPrice)
            };

            return View("Details", viewModel);
        }



        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            // Passing the TotalAmount as a data attribute
            ViewBag.TotalAmount = order.TotalAmount;
            ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName", order.ProductId);
            ViewBag.PaymentStatus = new SelectList(Enum.GetValues(typeof(PaymentStatus)), order.PaymentStatus);

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
   int id,
   [Bind("OrderId,ProductId,Quantity,PaymentStatus,OrderDate,CustomerName,PartialPaymentAmount,Barcode")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingOrder = await _context.Order.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (existingOrder == null)
                {
                    return NotFound();
                }

                var product = await _context.Inventory.FindAsync(order.ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                // Adjust stock quantity only
                int stockAdjustment = order.Quantity - existingOrder.Quantity;
                if (stockAdjustment > product.StockQuantity)
                {
                    ViewData["ErrorMessage"] = "Insufficient stock to update the order.";
                    ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName", order.ProductId);
                    ViewBag.PaymentStatus = new SelectList(Enum.GetValues(typeof(PaymentStatus)), order.PaymentStatus);
                    return View(order);
                }

                product.StockQuantity -= stockAdjustment;
                ApplyStockStatus(product);

                try
                {
                    // Update product stock quantity
                    _context.Update(product);

                    // Manually map only the allowed changes to the existing order
                    existingOrder.Quantity = order.Quantity;
                    existingOrder.PaymentStatus = order.PaymentStatus;
                    existingOrder.OrderDate = order.OrderDate;
                    existingOrder.CustomerName = order.CustomerName;
                    existingOrder.PartialPaymentAmount = order.PartialPaymentAmount;
                    existingOrder.Barcode = order.Barcode;

                    // Update the existing order without affecting UnitPrice or TotalAmount
                    _context.Update(existingOrder);
                    await _context.SaveChangesAsync();

                    TempData["LoginSuccess"] = "Order updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Order.Any(e => e.OrderId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index)); // Redirect to the appropriate page
            }

            ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName", order.ProductId);
            ViewBag.PaymentStatus = new SelectList(Enum.GetValues(typeof(PaymentStatus)), order.PaymentStatus);
            return View(order);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Remove the order
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();



            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";
            return RedirectToAction(nameof(Index)); // Redirect to the Orders list after deletion
        }

        public JsonResult GetProductDetails(int id)
        {
            var product = _context.Inventory.Find(id);
            if (product != null)
            {
                return Json(new
                {
                    productName = product.ProductName,
                    stockStatus = product.StockStatus.ToString(),
                    stockQuantity = product.StockQuantity,
                    unitPrice = product.UnitPrice,
                    barcode = product.Barcode
                });
            }
            return Json(null);
        }
    }
}
