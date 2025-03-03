using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using leo.Data;
using leo.Models;
using Microsoft.AspNetCore.Authorization;
using Twilio.Rest.Api.V2010.Account;
using Twilio;

using Twilio.Exceptions;
using OfficeOpenXml;
using leo.ViewModels;
using System.Net.Mail;
using System.Net;

namespace leo.Controllers
{
    [Authorize]


    public class InventoryController : Controller
    {
        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly string accountSid = "ACc0e7f08cb7409ec84fe6ff91d1c84fd1";
        private readonly string authToken = "d934277abaaedb3e006c6e577d52fb40";
            
        public InventoryController(leoContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }
        //[HttpPost]
        //public async Task<IActionResult> ImportFromExcel(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //    {
        //        return BadRequest("No file uploaded.");
        //    }

        //    var duplicateItems = new List<string>();

        //    using (var stream = new MemoryStream())
        //    {
        //        await file.CopyToAsync(stream);
        //        using (var package = new ExcelPackage(stream))
        //        {
        //            var worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first worksheet
        //            var rowCount = worksheet.Dimension.Rows;

        //            for (int row = 2; row <= rowCount; row++) // Start from 2 to skip header
        //            {
        //                var productName = worksheet.Cells[row, 1].Text;
        //                var categoryName = worksheet.Cells[row, 2].Text;
        //                var supplierName = worksheet.Cells[row, 3].Text;
        //                var realDate = DateTime.Parse(worksheet.Cells[row, 4].Text);
        //                var unitPrice = decimal.Parse(worksheet.Cells[row, 5].Text);
        //                var stockQuantity = int.Parse(worksheet.Cells[row, 6].Text);
        //                var description = worksheet.Cells[row, 7].Text;
        //                var stockStatus = worksheet.Cells[row, 8].Text;

        //                // Check for duplicates before adding
        //                var existingItem = _context.Inventory
        //                    .FirstOrDefault(i => i.ProductName == productName &&
        //                                         i.Category.CategoryName == categoryName &&
        //                                         i.SupplierProfile.Supplier == supplierName &&
        //                                         i.Date == realDate);

        //                if (existingItem != null)
        //                {
        //                    // Add duplicate item to the list
        //                    duplicateItems.Add(productName);
        //                    continue; // Skip the addition of this item
        //                }

        //                // Create a new inventory item
        //                var newItem = new Inventory
        //                {
        //                    ProductName = productName,
        //                    Category = _context.Category.FirstOrDefault(c => c.CategoryName == categoryName),
        //                    SupplierProfile = _context.SupplierProfile.FirstOrDefault(s => s.Supplier == supplierName),
        //                    Date = realDate,
        //                    UnitPrice = unitPrice,
        //                    StockQuantity = stockQuantity,
        //                    Description = description,
        //                    StockStatus = Enum.Parse<StockStatus>(stockStatus) // Ensure your enum type matches
        //                };

        //                _context.Inventory.Add(newItem);
        //            }

        //            await _context.SaveChangesAsync();
        //        }
        //    }

        //    // Store the duplicates in TempData for display in the view
        //    if (duplicateItems.Any())
        //    {
        //        TempData["DuplicateItems"] = string.Join(", ", duplicateItems);
        //    }

        //    return RedirectToAction("Index"); // Redirect back to the Index page after import
        //}


        public IActionResult GenerateInventoryReport()
        {
            //// Retrieve data for weekly and monthly sales
            //var weeklySalesItems = _context.Inventory
            //    .Include(i => i.Category)
            //    .Include(i => i.SupplierProfile)
            //    .Where(i => i.Date >= DateTime.Now.AddDays(-7)) // Sales within the last week
            //    .ToList();

            var monthlySalesItems = _context.Inventory
                .Include(i => i.Category)
                .Include(i => i.SupplierProfile)
                .Where(i => i.Date >= DateTime.Now.AddMonths(-1)) // Sales within the last month
                .ToList();

            //// Retrieve the email of the authenticated user
            //var preparedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;


            // Prepare the data model for the report view
            var reportData = new InventoryReportViewModel
            {
                //WeeklySalesItems = weeklySalesItems,
                MonthlySalesItems = monthlySalesItems,
                //PreparedBy = preparedBy
            };

            return View("InventoryReport", reportData);
        }


        //// Request Stock for the supplier when the product is low stock
        //public async Task<IActionResult> RequestStock(int productId)
        //{
        //    var product = await _context.Inventory.FirstOrDefaultAsync(p => p.ProductId == productId);

        //    // Check if stock is sufficient (above 10)
        //    if (product.StockQuantity > 10)
        //    {
        //        TempData["LoginSuccess"] = $"The product '{product.ProductName}' has sufficient stock.";
        //    }
        //    else
        //    {
        //        // Check if the supplier already exists for the product
        //        var existingSupplier = await _context.Supplier
        //            .AnyAsync(s => s.SupplierName == product.Suppliers && s.ProductsName == product.ProductName);

        //        if (!existingSupplier)
        //        {
        //            // Add a new supplier record if none exists
        //            var supplier = new Supplier
        //            {
        //                SupplierName = product.Suppliers,
        //                ProductsName = product.ProductName,
        //                Quantity = product.StockQuantity,
        //                Balance = 0
        //            };

        //            _context.Supplier.Add(supplier);
        //            await _context.SaveChangesAsync();

        //            TempData["LoginSuccess"] = $"Stock request for product '{product.ProductName}' was successful.";

        //            // Send SMS notification for stock request
        //            await SendSMS("9275311943", $"Stock request for {product.ProductName} with current stock quantity {product.StockQuantity}.");
        //        }
        //        else
        //        {
        //            TempData["LoginSuccess"] = $"The product '{product.ProductName}' already has an existing supplier record.";
        //        }
        //    }

        //    return RedirectToAction("Index", "Inventory");
        //}

        public async Task<IActionResult> RequestStock(int productId)
        {
            // Fetch the product from the Inventory table
            var product = await _context.Inventory
                .Include(p => p.SupplierProfile) // Ensure you load the related SupplierProfile
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Inventory");
            }

            // Check if stock is sufficient (above 10)
            if (product.StockQuantity > 10)
            {
                TempData["LoginSuccess"] = $"The product '{product.ProductName}' has sufficient stock.";
            }
            else
            {
                // Check if a supplier already exists for this product
                var existingSupplier = await _context.Supplier
                    .AnyAsync(s => s.SupplierName == product.SupplierProfile.Supplier &&
                                   s.ProductsName == product.ProductName);

                if (!existingSupplier)
                {
                    // Add a new supplier record if none exists
                    var supplier = new Supplier
                    {
                        SupplierName = product.SupplierProfile.Supplier,
                        ProductsName = product.ProductName,
                        Email = product.SupplierProfile.ContactEmail, // Use the SupplierProfile's email if available
                        Status = "Requested" // Set the Status to "Requested"
                    };

                    // Add the new supplier to the Supplier table
                    _context.Supplier.Add(supplier);
                    await _context.SaveChangesAsync();

                    TempData["LoginSuccess"] = $"Stock request for product '{product.ProductName}' was successful.";

                    //// Send SMS notification for stock request
                    await SendSMS("9666087724", $"Stock request for {product.ProductName} with current stock quantity {product.StockQuantity}.");
                }
                else
                {
                    TempData["LoginSuccess"] = $"The product '{product.ProductName}' already has an existing supplier record.";
                }
            }

            return RedirectToAction("Index", "Inventory");
        }


        //// Send SMS
        //public async Task<string> SendSMS(string phoneNumber, string messageBody)
        //{
        //    try
        //    {
        //        // Check if phoneNumber or messageBody is null
        //        if (string.IsNullOrEmpty(phoneNumber))
        //        {
        //            throw new ArgumentNullException(nameof(phoneNumber), "Phone number cannot be null or empty.");
        //        }

        //        if (string.IsNullOrEmpty(messageBody))
        //        {
        //            throw new ArgumentNullException(nameof(messageBody), "Message body cannot be null or empty.");
        //        }

        //        TwilioClient.Init(accountSid, authToken);

        //        var message = await MessageResource.CreateAsync(
        //            body: messageBody,
        //            from: new Twilio.Types.PhoneNumber("+12532043283"), // Twilio number
        //            to: new Twilio.Types.PhoneNumber("+63" + phoneNumber)
        //        );

        //        return message.Sid; // Return the SMS message SID for tracking
        //    }
        //    catch (ApiException ex)
        //    {
        //        // If there's an API authentication error, show this message instead
        //        if (ex.Message.Contains("Authenticate"))
        //        {
        //            return "Free trial API limit reached or authentication failed, but message displayed successfully.";
        //        }

        //        throw; // Rethrow if it's another error
        //    }
        //}

        public async Task<string> SendSMS(string phoneNumber, string messageBody)
        {
            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: messageBody,
                from: new Twilio.Types.PhoneNumber("+19789042835"), // Twilio number
                 to: new Twilio.Types.PhoneNumber("+63" + phoneNumber)
            );

            return message.Sid; // Return the SMS message SID for tracking
        }
        public async Task<IActionResult> Index(bool showDeleted = false)
        {
            ViewData["ShowDeleted"] = showDeleted;
            var productsQuery = _context.Inventory
                .Include(p => p.Category)
                .Include(p => p.SupplierProfile); // Ensure SupplierProfile is included

            // Retrieve products based on showDeleted flag
            var products = showDeleted
                ? await productsQuery.ToListAsync()
                : await productsQuery.Where(p => !p.IsDeleted).ToListAsync();

            // Notify if products are low or out of stock
            foreach (var product in products)
            {
                // Null check for product before sending notifications
                if (product == null) continue;

                var supplier = product.SupplierProfile;

                if (product.StockQuantity <= 5 && product.StockQuantity > 0) // Low stock check

                {
                    await SendSMS("9666087724", $"Alert: {product.ProductName} is LOW OF STOCK.");
                    if (supplier != null && !string.IsNullOrEmpty(product.ProductName))
                    {
                        // Configure and send the email for low stock
                        var smtpClient = new SmtpClient("smtp.gmail.com")
                        {
                            Port = 587,
                            Credentials = new NetworkCredential("dendopulgo123@gmail.com", "didegpdzeqmvnztj"),
                            EnableSsl = true,
                        };

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("leotech@gmail.com"),
                            Subject = "Leostore - Low Stock Alert",
                            Body = $"Dear {supplier.Supplier},\n\n" +
                                   $"This is to notify you that the product '{product.ProductName}' is low on stock with only {product.StockQuantity} units remaining.\n" +
                                   "Please take action to replenish the stock soon.\n\n",
                            IsBodyHtml = false,
                        };

                        mailMessage.To.Add("dendopulgo123@gmail.com");

                        try
                        {
                            await smtpClient.SendMailAsync(mailMessage);
                            TempData["SuccessMessage"] = $"Low stock email sent to {supplier.Supplier} for product '{product.ProductName}'.";
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = "There was an error sending the low stock email: " + ex.Message;
                        }
                    }
                }
                else if (product.StockQuantity == 0) // Out of stock check
                {
                    await SendSMS("9666087724", $"Alert: {product.ProductName} is OUT OF STOCK.");
                    if (supplier != null && !string.IsNullOrEmpty(product.ProductName))
                    {
                        // Configure and send the email for out of stock
                        var smtpClient = new SmtpClient("smtp.gmail.com")
                        {
                            Port = 587,
                            Credentials = new NetworkCredential("dendopulgo123@gmail.com", "didegpdzeqmvnztj"),
                            EnableSsl = true,
                        };

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("leotech@gmail.com"),
                            Subject = "Leostore - Stock Out Alert",
                            Body = $"Dear {supplier.Supplier},\n\n" +
                                   $"We regret to inform you that the product '{product.ProductName}' is currently out of stock.\n" +
                                   "Please update the stock status as soon as possible.\n\n",
                            IsBodyHtml = false,
                        };

                        mailMessage.To.Add("dendopulgo123@gmail.com");


                        try
                        {
                            await smtpClient.SendMailAsync(mailMessage);
                            TempData["SuccessMessage"] = $"Out of stock email sent to {supplier.Supplier} for product '{product.ProductName}'.";
                        }
                        catch (Exception ex)
                        {
                            TempData["ErrorMessage"] = "There was an error sending the out of stock email: " + ex.Message;
                        }
                    }
                }
            }

            return View(products);
        }

        //// GET: Products - Notify if products are low or out of stock
        //public async Task<IActionResult> Index(bool showDeleted = false)
        //{
        //    ViewData["ShowDeleted"] = showDeleted;
        //    var productsQuery = _context.Inventory
        //        .Include(p => p.Category)
        //        .Include(p => p.SupplierProfile); // Ensure SupplierProfile is included

        //    // Retrieve products based on showDeleted flag
        //    var products = showDeleted
        //        ? await productsQuery.ToListAsync()
        //        : await productsQuery.Where(p => !p.IsDeleted).ToListAsync();

        //    // Notify if products are low or out of stock
        //    foreach (var product in products)
        //    {
        //        // Null check for product before sending notifications
        //        if (product == null) continue;

        //        if (product.StockQuantity <= 5 && product.StockQuantity > 0) // Low stock check
        //        {
        //            //await SendSMS("9562947800", $"Alert: {product.ProductName} is low on stock with only {product.StockQuantity} units remaining.");
        //        }
        //        else if (product.StockQuantity == 0) // Out of stock check
        //        {
        //            //await SendSMS("9562947800", $"Alert: {product.ProductName} is OUT OF STOCK.");

        //            // Send email to the supplier if stock is 0
        //            if (product.SupplierProfile != null && !string.IsNullOrEmpty(product.ProductName))
        //            {
        //                var supplier = product.SupplierProfile;

        //                // Configure and send the email
        //                var smtpClient = new SmtpClient("smtp.gmail.com")
        //                {
        //                    Port = 587,
        //                    Credentials = new NetworkCredential("dendopulgo123@gmail.com", "didegpdzeqmvnztj"),
        //                    EnableSsl = true,
        //                };

        //                var mailMessage = new MailMessage
        //                {
        //                    From = new MailAddress("leotech@gmail.com"),
        //                    Subject = "Leostore - Stock Out Alert",
        //                    Body = $"Dear {product.SupplierProfile.Supplier},\n\n" +  // Assuming SupplierProfile is associated with the product
        //       $"We regret to inform you that the product '{product.ProductName}' is currently out of stock.\n" +
        //       "Please update the stock status as soon as possible.\n\n" ,

        //                    IsBodyHtml = false,
        //                };

        //                mailMessage.To.Add(supplier.ContactEmail); // Ensure this line doesn't throw if Email is null or empty


        //                try
        //                {
        //                    await smtpClient.SendMailAsync(mailMessage);

        //                    TempData["SuccessMessage"] = $"Email sent to {supplier.Product} for product '{product.ProductName}' stock request.";
        //                }
        //                catch (Exception ex)
        //                {
        //                    TempData["ErrorMessage"] = "There was an error sending the email: " + ex.Message;
        //                }
        //            }
        //        }
        //    }

        //    return View(products);
        //}



        // GET: Products - Notify if products are low or out of stock
        //public async Task<IActionResult> Index(bool showDeleted = false)
        //{
        //    ViewData["ShowDeleted"] = showDeleted;
        //    var productsQuery = _context.Inventory.Include(p => p.Category);

        //    // Retrieve products based on showDeleted flag
        //    var products = showDeleted
        //        ? await productsQuery.ToListAsync()
        //        : await productsQuery.Where(p => !p.IsDeleted).ToListAsync();

        //    //foreach (var product in products)
        //    //{

        //    //    if (product.StockQuantity <= 5 && product.StockQuantity > 0) // Low stock check
        //    //    {
        //    //        await SendSMS("9275311943", $"Alert: {product.ProductName} is low on stock with only {product.StockQuantity} units remaining.");
        //    //    }
        //    //    else if (product.StockQuantity == 0) // Out of stock check
        //    //    {
        //    //        await SendSMS("9275311943", $"Alert: {product.ProductName} is OUT OF STOCK.");
        //    //    }
        //    //}
        //    foreach (var product in products)
        //    {
        //        // Ensure product is not null before proceeding
        //        if (product == null || product.StockQuantity == null || product.ProductName == null)
        //        {
        //            continue; // Skip null or invalid products
        //        }

        //        // Always list the product (no matter the stock level)
        //        Console.WriteLine($"Product: {product.ProductName}, Stock Quantity: {product.StockQuantity}");

        //        // Check stock levels and send SMS notifications accordingly
        //        if (product.StockQuantity <= 5 && product.StockQuantity > 0) // Low stock check
        //        {
        //            await SendSMS("9275311943", $"Alert: {product.ProductName} is low on stock with only {product.StockQuantity} units remaining.");
        //        }
        //        else if (product.StockQuantity == 0) // Out of stock check
        //        {
        //            await SendSMS("9275311943", $"Alert: {product.ProductName} is OUT OF STOCK.");
        //        }
        //    }

        //    return View(products);
        //}

        //Example for checking low stock or out of stock and sending SMS
        //public async Task<IActionResult> Index(bool showDeleted = false)
        //{
        //    var products = await _context.Inventory.ToListAsync();

        //    foreach (var product in products)
        //    {
        //        if (product.StockQuantity <= 5 && product.StockQuantity > 0) // Low stock check
        //        {
        //            await SendSMS("9275311943", $"Alert: {product.ProductName} is low on stock with only {product.StockQuantity} units remaining.");
        //        }
        //        else if (product.StockQuantity == 0) // Out of stock check
        //        {
        //            await SendSMS("9275311943", $"Alert: {product.ProductName} is OUT OF STOCK.");
        //        }
        //    }

        //    return View(products);
        //}

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName");
            ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,CategoryId,Date,UnitPrice,StockQuantity,Description,ProfileId")] Inventory products)
        {
            if (ModelState.IsValid)
            {

                // Determine StockStatus based on StockQuantity
                if (products.StockQuantity <= 0)
                {
                    products.StockStatus = StockStatus.OutOfStock;
                }
                else if (products.StockQuantity < 5)
                {
                    products.StockStatus = StockStatus.LowStock;
                }
                else
                {
                    products.StockStatus = StockStatus.InStock;
                }

                _context.Add(products);
                await _context.SaveChangesAsync();
                // Set TempData for login success
                TempData["LoginSuccess"] = "Added successfully";


                await _auditLogService.LogActionAsync("Added", $"Inventory: '{products.ProductName}'Quantity: {products.StockQuantity}");




                return RedirectToAction(nameof(Create)); // Redirect to Index instead of Create
            }
            ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.SupplierProfile);
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
            return View(products);
        }
        //// POST: Products/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("ProductId,ProductName,CategoryId,Date,UnitPrice,StockQuantity,Description,ProfileId")] Inventory products)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (await _context.Inventory.AnyAsync(p => p.ProductName == products.ProductName))
        //        {
        //            ModelState.AddModelError("ProductName", "A product with this name already exists.");
        //            ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.ProfileId);
        //            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
        //            return View(products);
        //        }

        //        // Determine StockStatus based on StockQuantity
        //        if (products.StockQuantity <= 0)
        //        {
        //            products.StockStatus = StockStatus.OutOfStock;
        //        }
        //        else if (products.StockQuantity < 5)
        //        {
        //            products.StockStatus = StockStatus.LowStock;
        //        }
        //        else
        //        {
        //            products.StockStatus = StockStatus.InStock;
        //        }

        //        _context.Add(products);
        //        await _context.SaveChangesAsync();
        //        // Set TempData for login success
        //        TempData["LoginSuccess"] = "Added successfully";


        //        await _auditLogService.LogActionAsync("Added", $"Inventory: '{products.ProductName}'Quantity: {products.StockQuantity}");




        //        return RedirectToAction(nameof(Create)); // Redirect to Index instead of Create
        //    }
        //    ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.SupplierProfile);
        //    ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
        //    return View(products);
        //}

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Inventory == null)
            {
                return NotFound();
            }

            var products = await _context.Inventory.FindAsync(id);
            if (products == null)
            {
                return NotFound();
            }
            ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.ProfileId);
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
            return View(products);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,CategoryId,Date,UnitPrice,StockQuantity,Description,ProfileId")] Inventory products)
        {
            if (id != products.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Inventory.AnyAsync(p => p.ProductName == products.ProductName && p.ProductId != id))
                    {
                        ModelState.AddModelError("ProductName", "A product with this name already exists.");
                        ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.ProfileId);

                        ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
                        return View(products);
                    }

                    // Determine StockStatus based on StockQuantity
                    if (products.StockQuantity <= 0)
                    {
                        products.StockStatus = StockStatus.OutOfStock;
                    }
                    else if (products.StockQuantity < 5)
                    {
                        products.StockStatus = StockStatus.LowStock;
                    }
                    else
                    {
                        products.StockStatus = StockStatus.InStock;
                    }

                    _context.Update(products);
                    await _context.SaveChangesAsync();

                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Updated successfully";
                    await _auditLogService.LogActionAsync("Updated", $"Inventory: '{products.ProductName}'Quantity: {products.StockQuantity}");

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductsExists(products.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProfileId"] = new SelectList(_context.SupplierProfile, "ProfileId", "Supplier", products.ProfileId);
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
            return View(products);
        }


        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Inventory == null)
            {
                return NotFound();
            }

            var products = await _context.Inventory
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (products == null)
            {
                return NotFound();
            }

            return View(products);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Inventory.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Soft delete
            product.IsDeleted = true;
            await _context.SaveChangesAsync();

            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";

             await _auditLogService.LogActionAsync("Deleted", $"Inventory: '{product.ProductName}'Quantity: {product.StockQuantity}");

            return RedirectToAction(nameof(Index));
        }

        // This action can be called to permanently delete the product if needed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Inventory.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Hard delete
            _context.Inventory.Remove(product);
            await _context.SaveChangesAsync();


            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";
            return RedirectToAction(nameof(DeletedProducts)); // Adjust the redirect as necessary
        }


        public async Task<IActionResult> DeletedProducts()
        {
            var deletedProducts = await _context.Inventory
                .Where(p => p.IsDeleted)
                .ToListAsync();

            return View(deletedProducts);
        }

        public async Task<IActionResult> Restore(int id)
        {
            var product = await _context.Inventory.FindAsync(id);
            if (product == null || !product.IsDeleted)
            {
                return NotFound();
            }

            product.IsDeleted = false; // Restore the product
            _context.Inventory.Update(product);
            await _context.SaveChangesAsync();
            // Set success message

            // Set TempData for login success
            TempData["LoginSuccess"] = "Restored successfully";

            return RedirectToAction(nameof(DeletedProducts));
        }

        public async Task<IActionResult> DeletePermanent(int id)
        {
            var product = await _context.Inventory.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Inventory.Remove(product); // Permanently delete the product

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(DeletedProducts));
        }

        //// POST: Products/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var product = await _context.Products.FindAsync(id);
        //    if (product == null)
        //    {
        //        return NotFound();
        //    }

        //    // Mark the product as deleted
        //    product.IsDeleted = true;
        //    _context.Products.Update(product);
        //    await _context.SaveChangesAsync();

        //    // Log the deletion action
        //    await _auditLogService.LogActionAsync("Delete Product", $"Marked product as deleted: {product.ProductName} (ID: {product.ProductId})");

        //    return RedirectToAction(nameof(Index));
        //}




        private bool ProductsExists(int id)
        {
            return (_context.Inventory?.Any(e => e.ProductId == id)).GetValueOrDefault();
        }
    }
}
