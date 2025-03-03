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

namespace leo.Controllers
{
    [Authorize] // Uncomment this line to restrict access to admin role only
    public class SuppliersController : Controller
    {

        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService;
        private SupplierProfile supplierProfile;

        public SuppliersController(leoContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;

        }
        [HttpPost]
        public async Task<IActionResult> SendEmail(int supplierId)
        {
            // Find the supplier by ID
            var supplier = await _context.Supplier.FindAsync(supplierId);

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
                        <th>Products and Quantity </th>
                        <td>{supplier.ProductsAndQuantities ?? "Not Available"}</td>
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
                _context.TransactionHistory.Add(transaction);
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
            var supplier = _context.Supplier.Find(supplierId);

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
            _context.TransactionHistory.Add(transaction);
       
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
            var supplier = await _context.Supplier.FindAsync(supplierId);

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
            _context.TransactionHistory.Add(transaction);
            await _context.SaveChangesAsync(); // Save the transaction record

            // Redirect back to the list or details page
            return RedirectToAction("Index"); // Adjust as necessary
        }

        // GET: Suppliers  
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Supplier.ToListAsync(); // Get suppliers from the database

            // Map to SupplierViewModel
            var supplierViewModels = suppliers.Select(s => new SupplierViewModel
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                ProductName = s.ProductsName,
                Description = s.Description,
                ProductsAndQuantities = s.ProductsAndQuantities ?? "No items", // Default value if NULL
                Email = s.Email ,     // Fallback for null Email
                Quantity = s.Quantity,
                Status = s.Status,  // Ensure Status is mapped here
                //Balance = s.Balance,

            });

            return View(supplierViewModels); // Pass the list of view models to the view
        }
        public IActionResult Create(int? productId)
        {
            // If a product ID is provided, retrieve the product to pre-fill information (optional)
            Inventory product = null;
            if (productId.HasValue)
            {
                product = _context.Inventory.Find(productId);
                if (product == null)
                {
                    return NotFound();
                }
            }

            // Create and pass the SupplierViewModel to the view
            var viewModel = new SupplierViewModel
            {
                SupplierName = product?.SupplierProfile.Supplier, // Default empty string to enter a new supplier name
                ProductName = product?.ProductName, // Pre-fill product name if a product is found

                Quantity = 0, // Default value
                //Balance = 0.0M // Default value
            };

            return View(viewModel);
        }


        // Helper method to determine the transaction status based on the supplier's status
        private string GetTransactionStatusFromSupplier(Supplier supplier)
        {
            // Determine the transaction status based on the supplier's status
            switch (supplier.Status)
            {
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
        public async Task<IActionResult> Create(SupplierViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate supplier name
                if (await SupplierNameExists(viewModel.SupplierName))
                {
                    ModelState.AddModelError("SupplierName", "A supplier with this name already exists.");
                    return View(viewModel);
                }

                // Create a new Supplier entity
                var supplier = new Supplier
                {
                    SupplierName = viewModel.SupplierName,
                    ProductsName = viewModel.ProductName,
                    Quantity = viewModel.Quantity,
                    ProductsAndQuantities = viewModel.ProductsAndQuantities, // This now correctly binds the Products and Quantities
                    Description = viewModel.Description,
                    Email = viewModel.Email, // Ensure email is not null
                    Status = viewModel.Status
                };

                // Add the supplier to the context
                _context.Supplier.Add(supplier);
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
                    Quantity = supplier.Quantity, // This might need to be adjusted based on your logic
                    Description = supplier.Description,
                    TransactionType = GetTransactionStatusFromSupplier(supplier) // Ensure this method handles nulls
                };

                // Add the transaction to the context
                _context.TransactionHistory.Add(transaction);
                await _context.SaveChangesAsync(); // Save changes to persist the transaction

                // Log the action of adding the supplier
                await _auditLogService.LogActionAsync("Added", $"Supplier {supplier.SupplierName} was added with stock quantity {supplier.Quantity}.");

                // Set TempData for login success
                TempData["LoginSuccess"] = "Added successfully";

                // Redirect to the TransactionIndex view in the TransactionHistories controller
                return RedirectToAction("Create", "Suppliers");
            }

            return View(viewModel);
        }




        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Supplier == null)
            {
                return NotFound();
            }

            var supplier = await _context.Supplier.FindAsync(id);
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
               Email = supplier.Email,
               Description = supplier.Description,
                ProductName = supplier.ProductsName,
                Status = supplier.Status
            };

            return View(supplierViewModel);
        }


        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierViewModel supplierViewModel)
        {
            if (id != supplierViewModel.SupplierId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate supplier name
                    if (await SupplierNameExists(supplierViewModel.SupplierName, id))
                    {
                        ModelState.AddModelError("SupplierName", "A supplier with this name already exists.");
                        return View(supplierViewModel);
                    }

                    // Find the existing supplier entity
                    var supplier = await _context.Supplier.FindAsync(id);
                    if (supplier == null)
                    {
                        return NotFound();
                    }

                    // Preserve the Requested status if it exists
                    var existingSupplier = await _context.Supplier.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == id);
                    if (existingSupplier != null && existingSupplier.Status == "Requested")
                    {
                        supplierViewModel.Status = "Requested";
                    }

                    // Update the supplier details
                    supplier.SupplierName = supplierViewModel.SupplierName;
                    supplier.Quantity = supplierViewModel.Quantity;
                    supplier.Email = supplierViewModel.Email;
                    supplier.Description = supplierViewModel.Description;
                    supplier.ProductsName = supplierViewModel.ProductName;
                    supplier.Status = supplierViewModel.Status;

                    // Update stock quantity for the associated product
                    var product = await _context.Inventory.FindAsync(supplierViewModel.ProductId); // Assuming ProductId is in the ViewModel
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
                        Quantity = supplier.Quantity,
                        Description = supplier.Description,
                        // Set transaction type based on the supplier's status
                        TransactionType = GetTransactionStatusFromSupplier(supplier) // Dynamically set transaction type
                    };

                    // Add the transaction to the context
                    _context.TransactionHistory.Add(transaction);
                    await _context.SaveChangesAsync(); // Save changes to persist the transaction

                    // Log the supplier update action
                    await _auditLogService.LogActionAsync("Updated", $"Supplier: {supplier.SupplierName}, Quantity: {supplier.Quantity}");

                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Updated successfully";
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

            // Return the view with the ViewModel if validation fails
            return View(supplierViewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Supplier == null)
            {
                return Problem("Entity set 'leoContext.Supplier' is null.");
            }

            // Find the supplier by id
            var supplier = await _context.Supplier.FindAsync(id);

            // If no supplier is found, return NotFound
            if (supplier == null)
            {
                return NotFound();
            }

            // Update the related TransactionHistory records to set SupplierId to NULL or a placeholder
            var transactions = _context.TransactionHistory.Where(t => t.SupplierId == supplier.SupplierId).ToList();
            foreach (var transaction in transactions)
            {
                transaction.SupplierId = null;  // Set SupplierId to null
            
                _context.TransactionHistory.Update(transaction);  // Mark the updated transactions
            }

            // Remove the supplier permanently
            _context.Supplier.Remove(supplier);

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
            return await _context.Supplier
                .AnyAsync(s => s.SupplierName == supplierName && (excludedSupplierId == null || s.SupplierId != excludedSupplierId));
        }
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            // Find the supplier by ID
            var supplier = await _context.Supplier.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            // Find the product associated with the supplier's product name
            var product = await _context.Inventory.FirstOrDefaultAsync(p => p.ProductName == supplier.ProductsName);
            if (product == null)
            {
                return NotFound();
            }

            // Update product stock quantity with the supplier's quantity
            product.StockQuantity += supplier.Quantity;

            // Reset supplier's quantity after approval (optional)
            supplier.Quantity = 0;

            // Create a new transaction history entry
            var transaction = new TransactionHistory
            {
                SupplierId = supplier.SupplierId,
                Date = DateTime.Now,
                Amount = product.StockQuantity, // Log the updated stock quantity or any other relevant amount
                ProductType = product.ProductName,
                TransactionDate = DateTime.Now,
                TransactionType = "Approved" // Set the type of transaction as "Approved"
            };

            // Add the transaction history entry to the context
            _context.TransactionHistory.Add(transaction);

            // Delete the supplier from the context
            _context.Supplier.Remove(supplier);

            // Save changes to both the product, transaction history, and supplier
            await _context.SaveChangesAsync();

            // Log the approval action
            await _auditLogService.LogActionAsync("Approved Supplier", $"{supplier.SupplierName} Product: '{product.ProductName}'");

            // Provide user feedback with TempData
            TempData["LoginSuccess"] = $"Approved supplier {supplier.SupplierName} Product: {product.ProductName}";

            // Redirect to the index page
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Supplier?.Any(e => e.SupplierId == id) ?? false;
        }




    }
}
