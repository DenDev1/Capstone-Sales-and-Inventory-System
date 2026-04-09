using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using leo.Data;
using leo.Models;
using Microsoft.AspNetCore.Authorization;
using leo.ViewModels;
using leo.Services;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;

namespace leo.Controllers
{
    [Authorize] // Uncomment this line to restrict access to admin role only
    public class SuppliersController : Controller
    {

        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService;
        private DbSet<Supplier> Suppliers => _context.Supplier!;
        private DbSet<Inventory> InventoryItems => _context.Inventory!;
        private DbSet<TransactionHistory> Transactions => _context.TransactionHistory!;

        public SuppliersController(leoContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;

        }
        [HttpPost]
        public async Task<IActionResult> SendEmail(int supplierId)
        {
            // Find the supplier by ID
            var supplier = await Suppliers.FindAsync(supplierId);

            if (supplier == null)
            {
                return NotFound();
            }

            // Check if the supplier's email is null or empty
            if (string.IsNullOrEmpty(supplier.Email))
            {
                TempData["ErrorMessage"] = "The supplier's email address is required.";
                return RedirectToAction(nameof(Index));
            }

            // Send the email (you can replace this with an email service like SendGrid)
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("dendopulgo123@gmail.com", "didegpdzeqmvnztj"),
                EnableSsl = true,
            };

            var requestedItems = ParseProductsAndQuantitiesText(supplier.ProductsAndQuantities);
            if (requestedItems.Count == 0)
            {
                requestedItems.Add((supplier.ProductsName, supplier.Quantity));
            }

            var itemsRowsHtml = string.Join("",
                requestedItems.Select(i =>
                    $"<tr><td>{WebUtility.HtmlEncode(i.ProductName)}</td><td>{i.Quantity}</td></tr>"));
            var mailMessage = new MailMessage
            {
                From = new MailAddress("leotech@gmail.com"),
                Subject = "LEOTECH COMPUTER STORE",
                Body = $@"
        <html>
            <head>
                <style>
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                        font-family: Arial, sans-serif;
                    }}
                    th, td {{
                        border: 1px solid #dddddd;
                        text-align: left;
                        padding: 8px;
                    }}
                    th {{
                        background-color: #f2f2f2;
                    }}
                    tr:nth-child(even) {{
                        background-color: #f9f9f9;
                    }}

                </style>
            </head>
            <body>
                <p>Dear {supplier.SupplierName},</p>
                <p>This is a notification email. Please find the details below:</p>
                <table>
              
                
                    <tr>
                        <th>Requested Items</th>
                        <td>
                            <table>
                                <thead>
                                    <tr><th>Product</th><th>Quantity</th></tr>
                                </thead>
                                <tbody>
                                    {itemsRowsHtml}
                                </tbody>
                            </table>
                        </td>
                    </tr>
   <tr>
                        <th>Description </th>
                        <td>{supplier.Description}</td>
                    </tr>
                </table>
                <p>
                    <a href='{Url.Action("UpdateStatusToInTransit", "Suppliers", new { supplierId = supplier.SupplierId, redirectToGmail = true }, Request.Scheme)}' 
                       style='display: inline-block; padding: 10px 20px; font-size: 16px; color: white; background-color: #007bff; text-decoration: none; border-radius: 5px;'>
                       Click here to confirm the status update to In Transit
                    </a>
                </p>
                <p>Thank you,<br>LEOTECH Computer Store</p>
            </body>
        </html>",
                IsBodyHtml = true  // Ensure this is true to allow clickable links and HTML rendering
            };


            try
            {
                mailMessage.To.Add(supplier.Email); // Ensure this line doesn't throw if Email is null or empty

                await smtpClient.SendMailAsync(mailMessage);

                // Update the supplier's status to "Pending"
                supplier.Status = "Pending";
                // Update supplier to reflect that the email was sent

                _context.Update(supplier);
                await _context.SaveChangesAsync();

                // Log a transaction for the email send action
                var transaction = new TransactionHistory
                {
                    SupplierId = supplier.SupplierId,
                    Date = DateTime.Now,
                    Description = supplier.Description,
                    ProductsAndQuantities = supplier.ProductsAndQuantities,
                    Quantity = supplier.Quantity,
                    //Amount = supplier.Balance, // You can choose the appropriate value for Amount
                    ProductType = supplier.ProductsName, // Assuming this property exists
                    TransactionDate = DateTime.Now,
                    TransactionType = "Pending" // Customize this as needed
                };

                // Add the transaction history to the database
                Transactions.Add(transaction);
                await _context.SaveChangesAsync(); // Save the transaction record

                // Set TempData for success message
                TempData["LoginSuccess"] = "Sent Email successfully";

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "There was an error sending the email: " + ex.Message;
            }

            // Redirect back to the index or another page
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public IActionResult UpdateStatusToInTransit(int supplierId, bool redirectToGmail = false)
        {
            // Find the supplier by ID
            var supplier = Suppliers.Find(supplierId);

            if (supplier == null)
            {
                return NotFound();
            }
            // Log a transaction for the 'Delivered' status
            var transaction = new TransactionHistory
            {
                SupplierId = supplier.SupplierId,
                Date = DateTime.Now,
                Quantity = supplier.Quantity,
                ProductsAndQuantities = supplier.ProductsAndQuantities,
                //Amount = supplier.Balance,  // Assuming this is the correct amount, replace as needed
                ProductType = supplier.ProductsName, // Assuming ProductsName exists in Supplier model
                TransactionDate = DateTime.Now,
                TransactionType = "Notice" // Transaction type is set to 'Delivered' here
            };

            // Add the transaction history to the database
            Transactions.Add(transaction);
       
            // Update the supplier's status to 'In Transit'
            supplier.Status = "Notice";
            _context.SaveChanges();  // Ensure the changes are committed to the database

            // Optionally, add a confirmation message in TempData
            TempData["LoginSuccess"] = "Supplier status updated to In Transit.";

    

            // If the redirectToGmail flag is true, redirect to Gmail's compose page
            if (redirectToGmail)
            {
                var gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to={supplier.Email}&su=Leostore%20Status%20Update&body=Please%20confirm%20the%20status%20update%20to%20In%20Transit.";
                return Redirect(gmailUrl); // Redirect to Gmail's compose page
            }

            // Redirect to the suppliers list page or any other appropriate page
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> MarkAsDelivered(int supplierId)
        {
            // Find the supplier by ID
            var supplier = await Suppliers.FindAsync(supplierId);

            if (supplier == null)
            {
                return NotFound();
            }

            // Update the status to 'Delivered'
            supplier.Status = "Delivered";
            _context.Update(supplier);
            await _context.SaveChangesAsync();

            // Log a transaction for the status change
            var transaction = new TransactionHistory
            {
                SupplierId = supplier.SupplierId,
                Date = DateTime.Now,
                //Amount = supplier.Balance, // You can choose the appropriate value for the Amount
                ProductType = supplier.ProductsName, // Assuming this property exists
                TransactionDate = DateTime.Now,
                TransactionType = "Delivered" // Customize this as needed
            };

            // Add the transaction history to the database
            Transactions.Add(transaction);
            await _context.SaveChangesAsync(); // Save the transaction record
            return RedirectToAction("Index");
        }

        // GET: Suppliers  
        public async Task<IActionResult> Index()
        {
            // Use AsNoTracking for read-only view model mapping
            var suppliers = await Suppliers
                .AsNoTracking()
                .ToListAsync();

            var supplierViewModels = suppliers.Select(s => new SupplierViewModel
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                ProductName = s.ProductsName,
                Description = s.Description,
                ProductsAndQuantities = s.ProductsAndQuantities ?? "No items",
                Email = s.Email ,
                Quantity = s.Quantity,
                UnitPrice = s.UnitPrice,
                Status = s.Status,
            });

            return View(supplierViewModels);
        }
        public IActionResult Create(int? productId)
        {
            // If a product ID is provided, retrieve the product to pre-fill information (optional)
            Inventory? product = null;
            if (productId.HasValue)
            {
                product = InventoryItems.Find(productId);
                if (product == null)
                {
                    return NotFound();
                }
            }

            // Create and pass the SupplierCreateViewModel to the view
            var viewModel = new SupplierCreateViewModel
            {
                SupplierName = string.Empty, // Enter a new supplier name
                Email = string.Empty,
                UnitPrice = 1m
            };

            if (product != null)
            {
                viewModel.LineItems.Add(new SupplierLineItemViewModel
                {
                    ProductName = product.ProductName,
                    Quantity = 1
                });
            }

            return View(viewModel);
        }


        // Helper method to determine the transaction status based on the supplier's status
        private string GetTransactionStatusFromSupplier(Supplier supplier)
        {
            // Determine the transaction status based on the supplier's status
            switch (supplier.Status)
            {
                case "Requested":
                    return "Requested";
                case "Pending":
                    return "Pending"; // If the supplier status is 'Pending'
                case "Delivered":
                    return "Delivered"; // If the supplier status is 'Delivered'
                case "Notice":
                    return "Notice"; // If the supplier status is 'InTransit'
                default:
                    return "Unknown"; // Default status if no match found
            }
        }


        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierCreateViewModel viewModel)
        {
            var isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var normalizedItems = NormalizeLineItems(viewModel.LineItems);
            if (normalizedItems.Count == 0)
            {
                if (isAjaxRequest)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please add at least one product and quantity."
                    });
                }

                ModelState.AddModelError(nameof(viewModel.LineItems), "Please add at least one product and quantity.");
                return View(viewModel);
            }

            viewModel.SupplierName = await ResolveSupplierNameForCreateAsync(viewModel.SupplierName);
            viewModel.Email = NormalizeEmailOrDefault(viewModel.Email);
            viewModel.UnitPrice = viewModel.UnitPrice > 0 ? viewModel.UnitPrice : 1m;

            try
            {
                var productsAndQuantities = BuildProductsAndQuantitiesText(normalizedItems);

                // Create a new Supplier entity
                var supplier = new Supplier
                {
                    SupplierName = viewModel.SupplierName,
                    ProductsName = normalizedItems[0].ProductName, // legacy single-item field
                    Quantity = normalizedItems[0].Quantity, // legacy single-item field
                    UnitPrice = viewModel.UnitPrice,
                    Balance = viewModel.UnitPrice * normalizedItems.Sum(i => i.Quantity),
                    ProductsAndQuantities = productsAndQuantities,
                    Description = viewModel.Description ?? string.Empty,
                    Email = viewModel.Email, // Ensure email is not null
                    Status = "Requested"
                };

                // Add the supplier to the context
                Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // Log the creation of a supplier
                await _auditLogService.LogActionAsync("Supplier Created", $"Supplier {viewModel.SupplierName} created.");

                // Add the transaction for this supplier
                var transaction = new TransactionHistory
                {
                    SupplierId = supplier.SupplierId, // Associate with the newly created supplier
                    Date = DateTime.Now, // Current date
                    ProductsAndQuantities = supplier.ProductsAndQuantities, // Bind the Products and Quantities to the transaction
                    ProductType = supplier.ProductsName,
                    TransactionDate = DateTime.Now,
                    Quantity = normalizedItems.Sum(i => i.Quantity),
                    Description = supplier.Description,
                    TransactionType = GetTransactionStatusFromSupplier(supplier) // Ensure this method handles nulls
                };

                // Add the transaction to the context
                Transactions.Add(transaction);
                await _context.SaveChangesAsync(); // Save changes to persist the transaction

                // Log the action of adding the supplier
                await _auditLogService.LogActionAsync("Added", $"Supplier {supplier.SupplierName} was added with stock quantity {supplier.Quantity}.");

                // Set TempData for login success
                TempData["LoginSuccess"] = "Added successfully";

                if (isAjaxRequest)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Supplier added successfully."
                    });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (isAjaxRequest)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = ex.InnerException?.Message ?? ex.Message
                    });
                }

                ModelState.AddModelError(string.Empty, "Unable to save supplier.");
                return View(viewModel);
            }
        }

        private async Task<string> ResolveSupplierNameForCreateAsync(string? supplierName)
        {
            var candidate = string.IsNullOrWhiteSpace(supplierName)
                ? "Unknown Supplier"
                : supplierName.Trim();

            while (await SupplierNameExists(candidate))
            {
                candidate = $"{candidate} Copy";
            }

            return candidate;
        }

        private static string NormalizeEmailOrDefault(string? email)
        {
            var trimmed = (email ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                try
                {
                    _ = new MailAddress(trimmed);
                    return trimmed;
                }
                catch
                {
                    // fallback below
                }
            }

            return $"unknown.supplier.{DateTime.UtcNow:yyyyMMddHHmmssfff}@placeholder.local";
        }

        private static List<(string ProductName, int Quantity)> NormalizeLineItems(IEnumerable<SupplierLineItemViewModel> lineItems)
        {
            if (lineItems == null)
            {
                return new List<(string ProductName, int Quantity)>();
            }

            return lineItems
                .Where(i => i != null)
                .Select(i => (ProductName: (i.ProductName ?? string.Empty).Trim(), Quantity: i.Quantity))
                .Where(i => !string.IsNullOrWhiteSpace(i.ProductName) && i.Quantity > 0)
                .GroupBy(i => i.ProductName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (ProductName: g.First().ProductName, Quantity: g.Sum(x => x.Quantity)))
                .OrderBy(i => i.ProductName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildProductsAndQuantitiesText(IReadOnlyList<(string ProductName, int Quantity)> items)
        {
            if (items == null || items.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, items.Select(i => $"{i.ProductName} | {i.Quantity}"));
        }

        private static List<(string ProductName, int Quantity)> ParseProductsAndQuantitiesText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<(string ProductName, int Quantity)>();
            }

            var results = new List<(string ProductName, int Quantity)>();
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                // Skip header-like lines
                if (line.Contains("Product", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("Quantity", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = line.Split('|');
                if (parts.Length < 2)
                {
                    continue;
                }

                var productName = parts[0].Trim();
                var qtyText = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(productName))
                {
                    continue;
                }

                if (!int.TryParse(qtyText, out var quantity) || quantity < 1)
                {
                    continue;
                }

                results.Add((productName, quantity));
            }

            return results;
        }




        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            // Create SupplierViewModel and map data
            var supplierViewModel = new SupplierViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                Quantity = supplier.Quantity,
                UnitPrice = supplier.UnitPrice,
                Email = supplier.Email,
                Description = supplier.Description,
                ProductName = supplier.ProductsName,
                ProductsAndQuantities = supplier.ProductsAndQuantities,
                LineItems = ParseProductsAndQuantitiesText(supplier.ProductsAndQuantities)
                    .Select(i => new SupplierLineItemViewModel
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity
                    })
                    .ToList(),
                Status = supplier.Status
            };

            if (supplierViewModel.LineItems.Count == 0 &&
                !string.IsNullOrWhiteSpace(supplier.ProductsName) &&
                supplier.Quantity > 0)
            {
                supplierViewModel.LineItems.Add(new SupplierLineItemViewModel
                {
                    ProductName = supplier.ProductsName,
                    Quantity = supplier.Quantity
                });
            }

            return View(supplierViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetEditData(int id)
        {
            var supplier = await Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Supplier not found."
                });
            }

            var lineItems = ParseProductsAndQuantitiesText(supplier.ProductsAndQuantities)
                .Select(i => new SupplierLineItemViewModel
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity
                })
                .ToList();

            if (lineItems.Count == 0 &&
                !string.IsNullOrWhiteSpace(supplier.ProductsName) &&
                supplier.Quantity > 0)
            {
                lineItems.Add(new SupplierLineItemViewModel
                {
                    ProductName = supplier.ProductsName,
                    Quantity = supplier.Quantity
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    supplierId = supplier.SupplierId,
                    supplierName = supplier.SupplierName,
                    email = supplier.Email,
                    description = supplier.Description,
                    unitPrice = supplier.UnitPrice,
                    status = supplier.Status,
                    lineItems
                }
            });
        }


        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierViewModel supplierViewModel)
        {
            var isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (id != supplierViewModel.SupplierId)
            {
                return NotFound();
            }

            var normalizedItems = NormalizeLineItems(supplierViewModel.LineItems);
            if (normalizedItems.Count == 0 &&
                !string.IsNullOrWhiteSpace(supplierViewModel.ProductName) &&
                supplierViewModel.Quantity > 0)
            {
                normalizedItems = NormalizeLineItems(new[]
                {
                    new SupplierLineItemViewModel
                    {
                        ProductName = supplierViewModel.ProductName,
                        Quantity = supplierViewModel.Quantity
                    }
                });
            }

            if (normalizedItems.Count == 0)
            {
                ModelState.AddModelError(nameof(supplierViewModel.LineItems), "Please add at least one product and quantity.");
            }

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    var errors = ModelState
                        .Where(kvp => kvp.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                    return BadRequest(new
                    {
                        success = false,
                        message = "Please fix the validation errors and try again.",
                        errors
                    });
                }

                return View(supplierViewModel);
            }

            try
            {
                // Check for duplicate supplier name
                if (await SupplierNameExists(supplierViewModel.SupplierName, id))
                {
                    ModelState.AddModelError("SupplierName", "A supplier with this name already exists.");

                    if (isAjaxRequest)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "A supplier with this name already exists."
                        });
                    }

                    return View(supplierViewModel);
                }

                // Find the existing supplier entity
                var supplier = await Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    return NotFound();
                }

                // Preserve the Requested status if it exists
                var existingSupplier = await Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == id);
                if (existingSupplier != null && existingSupplier.Status == "Requested")
                {
                    supplierViewModel.Status = "Requested";
                }

                // Update the supplier details
                supplier.SupplierName = supplierViewModel.SupplierName;
                supplier.Quantity = normalizedItems[0].Quantity;
                supplier.UnitPrice = supplierViewModel.UnitPrice;
                supplier.Balance = supplierViewModel.UnitPrice * normalizedItems.Sum(i => i.Quantity);
                supplier.Email = supplierViewModel.Email;
                supplier.Description = supplierViewModel.Description;
                supplier.ProductsName = normalizedItems[0].ProductName;
                supplier.ProductsAndQuantities = BuildProductsAndQuantitiesText(normalizedItems);
                supplier.Status = supplierViewModel.Status;

                // Update stock quantity for the associated product
                var product = await InventoryItems.FindAsync(supplierViewModel.ProductId); // Assuming ProductId is in the ViewModel
                if (product != null)
                {
                    product.StockQuantity += supplierViewModel.Quantity; // Add the updated quantity to stock
                    _context.Update(product); // Update the product in the context
                }

                // Update the supplier in the database
                _context.Update(supplier);
                await _context.SaveChangesAsync(); // Save changes

                // Automatically add a transaction history record after update
                var transaction = new TransactionHistory
                {
                    SupplierId = supplier.SupplierId, // Associate with the supplier being updated
                    Date = DateTime.Now, // Current date
                    ProductType = supplier.ProductsName,
                    TransactionDate = DateTime.Now,
                    Quantity = normalizedItems.Sum(i => i.Quantity),
                    Description = supplier.Description,
                    ProductsAndQuantities = supplier.ProductsAndQuantities,
                    // Set transaction type based on the supplier's status
                    TransactionType = GetTransactionStatusFromSupplier(supplier) // Dynamically set transaction type
                };

                // Add the transaction to the context
                Transactions.Add(transaction);
                await _context.SaveChangesAsync(); // Save changes to persist the transaction

                // Log the supplier update action
                await _auditLogService.LogActionAsync("Updated", $"Supplier: {supplier.SupplierName}, Quantity: {supplier.Quantity}");

                // Set TempData for login success
                TempData["LoginSuccess"] = "Updated successfully";

                if (isAjaxRequest)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Supplier updated successfully."
                    });
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(supplierViewModel.SupplierId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Redirect to the index page after successful edit
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            

            // Find the supplier by id
            var supplier = await Suppliers.FindAsync(id);

            // If no supplier is found, return NotFound
            if (supplier == null)
            {
                return NotFound();
            }

            // Update the related TransactionHistory records to set SupplierId to NULL or a placeholder
            var transactions = Transactions.Where(t => t.SupplierId == supplier.SupplierId).ToList();
            foreach (var transaction in transactions)
            {
                transaction.SupplierId = null;  // Set SupplierId to null
            
                Transactions.Update(transaction);  // Mark the updated transactions
            }

            // Remove the supplier permanently
            Suppliers.Remove(supplier);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";

            // Optionally, log the delete action
            await _auditLogService.LogActionAsync("Deleted Supplier", $" {supplier.SupplierName}");

            // Redirect to the Index action (list of suppliers) after deletion
            return RedirectToAction(nameof(Index));
        }


        // Check if a supplier with the same name already exists (with optional ID exclusion)
        private async Task<bool> SupplierNameExists(string supplierName, int? excludedSupplierId = null)
        {
            return await Suppliers
                .AnyAsync(s => s.SupplierName == supplierName && (excludedSupplierId == null || s.SupplierId != excludedSupplierId));
        }
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            // Find the supplier by ID
            var supplier = await Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            var requestedItems = ParseProductsAndQuantitiesText(supplier.ProductsAndQuantities);
            if (requestedItems.Count == 0)
            {
                requestedItems.Add((supplier.ProductsName, supplier.Quantity));
            }

            var missingProducts = new List<string>();
            var totalAdded = 0;

            foreach (var item in requestedItems)
            {
                var product = await InventoryItems.FirstOrDefaultAsync(p => p.ProductName == item.ProductName);
                if (product == null)
                {
                    missingProducts.Add(item.ProductName);
                    continue;
                }

                product.StockQuantity += item.Quantity;
                totalAdded += item.Quantity;
            }

            if (missingProducts.Count > 0)
            {
                TempData["ErrorMessage"] = $"Cannot approve. Missing inventory products: {string.Join(", ", missingProducts)}";
                return RedirectToAction(nameof(Index));
            }

            // Create a new transaction history entry
            var transaction = new TransactionHistory
            {
                SupplierId = supplier.SupplierId,
                Date = DateTime.Now,
                Amount = totalAdded,
                ProductType = requestedItems.Count == 1 ? requestedItems[0].ProductName : "Multiple",
                TransactionDate = DateTime.Now,
                TransactionType = "Approved",
                Quantity = totalAdded,
                ProductsAndQuantities = supplier.ProductsAndQuantities
            };

            // Add the transaction history entry to the context
            Transactions.Add(transaction);

            // Delete the supplier from the context
            Suppliers.Remove(supplier);

            // Save changes to both the product, transaction history, and supplier
            await _context.SaveChangesAsync();

            // Log the approval action
            await _auditLogService.LogActionAsync("Approved Supplier", $"{supplier.SupplierName} Items: {transaction.ProductType}");

            // Provide user feedback with TempData
            TempData["LoginSuccess"] = $"Approved supplier {supplier.SupplierName} (+{totalAdded} stocks)";

            // Redirect to the index page
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return Suppliers?.Any(e => e.SupplierId == id) ?? false;
        }




    }
}

