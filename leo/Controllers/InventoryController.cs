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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace leo.Controllers
{
    [Authorize]


    public class InventoryController : Controller
    {
        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string accountSid = "ACc0e7f08cb7409ec84fe6ff91d1c84fd1";
        private readonly string authToken = "d934277abaaedb3e006c6e577d52fb40";
            
        public InventoryController(leoContext context, AuditLogService auditLogService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _auditLogService = auditLogService;
            _webHostEnvironment = webHostEnvironment;
        }
     


     
        public async Task<IActionResult> RequestStock(int productId)
        {
            // Fetch the product from the Inventory table
            if (_context.Inventory == null)
            {
                TempData["Error"] = "Database error.";
                return RedirectToAction("Index", "Inventory");
            }

            var product = await _context.Inventory
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
                // Add stock request without supplier profile reference
                var supplier = new Supplier
                {
                    SupplierName = "Pending",
                    ProductsName = product.ProductName,
                    Email = "pending@leostore.com",
                    Status = "Requested"
                };

                if (_context.Supplier != null)
                {
                    _context.Supplier.Add(supplier);
                    await _context.SaveChangesAsync();
                }

                TempData["LoginSuccess"] = $"Stock request for product '{product.ProductName}' was successful.";

                //// Send SMS notification for stock request
                //await SendSMS("9666087724", $"Stock request for {product.ProductName} with current stock quantity {product.StockQuantity}.");
            }

            return RedirectToAction("Index", "Inventory");
        }


        // Send SMS
        public async Task<string> SendSMS(string phoneNumber, string messageBody)
        {
            try
            {
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(messageBody))
                {
                    return string.Empty;
                }

                TwilioClient.Init(accountSid, authToken);

                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new Twilio.Types.PhoneNumber("+12532043283"), // Twilio number
                    to: new Twilio.Types.PhoneNumber("+63" + phoneNumber)
                );

                return message.Sid;
            }
            catch (ApiException ex)
            {
                if (ex.Message.Contains("Authenticate"))
                {
                    return "Trial limit reached or authentication failed.";
                }
                throw;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task<IActionResult> Index(bool showDeleted = false)
        {
            ViewData["ShowDeleted"] = showDeleted;
            if (_context.Inventory == null)
            {
                return NotFound();
            }

            // High-performance read using AsNoTracking
            var productsQuery = _context.Inventory
                .AsNoTracking()
                .Include(p => p.Category);

            var products = await productsQuery
                .Where(p => showDeleted || !p.IsDeleted)
                .ToListAsync();
            
            return View(products);
        }
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
            // ProfileId removed - SupplierProfile no longer used
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,CategoryId,Date,UnitPrice,StockQuantity,Description,ProfileId,Barcode,Suppliers")] Inventory products, IFormFile? imageFile)
        {
            products.ProductName = products.ProductName?.Trim() ?? string.Empty;
            products.Description = products.Description?.Trim() ?? string.Empty;
            products.Barcode = products.Barcode?.Trim() ?? string.Empty;
            products.Suppliers = products.Suppliers?.Trim() ?? string.Empty;

            if (products.Date == default)
            {
                products.Date = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                try
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

                    if (string.IsNullOrWhiteSpace(products.Suppliers))
                    {
                        products.Suppliers = products.Barcode;
                    }

                    products.ImagePath = await SaveProductImageAsync(imageFile);

                    _context.Add(products);
                    await _context.SaveChangesAsync();
                    TempData["LoginSuccess"] = "Added successfully";

                    await _auditLogService.LogActionAsync("Added", $"Inventory: '{products.ProductName}'Quantity: {products.StockQuantity}");

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
                    {
                        return Json(new { success = true, message = "Inventory item created successfully!" });
                    }

                    return RedirectToAction(nameof(Create));
                }
                catch (DbUpdateException ex)
                {
                    var dbError = ex.InnerException?.Message ?? ex.Message;

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
                    {
                        return StatusCode(500, new { success = false, message = dbError });
                    }

                    ModelState.AddModelError(string.Empty, dbError);
                }
            }
            // ProfileId removed - SupplierProfile no longer used
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
            
            // Check if it's an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }
            
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
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "CategoryName", products.CategoryId);
            return View(products);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,CategoryId,Date,UnitPrice,StockQuantity,Description,ProfileId,Barcode,Suppliers,ImagePath")] Inventory products, IFormFile? imageFile)
        {
            if (id != products.ProductId)
            {
                return NotFound();
            }

            products.ProductName = products.ProductName?.Trim() ?? string.Empty;
            products.Description = products.Description?.Trim() ?? string.Empty;
            products.Barcode = products.Barcode?.Trim() ?? string.Empty;
            products.Suppliers = products.Suppliers?.Trim() ?? string.Empty;

            if (products.Date == default)
            {
                products.Date = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (_context.Inventory != null && await _context.Inventory.AnyAsync(p => p.ProductName == products.ProductName && p.ProductId != id))
                    {
                        ModelState.AddModelError("ProductName", "A product with this name already exists.");
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

                    if (string.IsNullOrWhiteSpace(products.Suppliers))
                    {
                        products.Suppliers = products.Barcode;
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        products.ImagePath = await SaveProductImageAsync(imageFile);
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
            if (_context.Inventory == null)
            {
                return NotFound();
            }

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
            if (_context.Inventory == null)
            {
                return NotFound();
            }

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
            if (_context.Inventory == null)
            {
                return NotFound();
            }

            var deletedProducts = await _context.Inventory
                .Where(p => p.IsDeleted)
                .ToListAsync();

            return View(deletedProducts);
        }

        public async Task<IActionResult> Restore(int id)
        {
            if (_context.Inventory == null)
            {
                return NotFound();
            }

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
            if (_context.Inventory == null)
            {
                return NotFound();
            }

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

        // Category Management Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            categoryName = categoryName.Trim();

            // Validate that category name contains only letters
            if (!System.Text.RegularExpressions.Regex.IsMatch(categoryName, @"^[a-zA-Z]+$"))
            {
                return Json(new { success = false, message = "Category name can only contain letters." });
            }

            if (await _context.Category.AnyAsync(c => c.CategoryName == categoryName))
            {
                return Json(new { success = false, message = "A category with this name already exists." });
            }

            try
            {
                var category = new Category { CategoryName = categoryName };
                _context.Add(category);
                await _context.SaveChangesAsync();
                
                // Try to log, but don't fail if audit logging fails
                try
                {
                    await _auditLogService.LogActionAsync("Create", $"Category: {category.CategoryName}");
                }
                catch { /* Audit logging failed, but category was created */ }
                
                return Json(new { success = true, message = "Category added successfully.", categoryId = category.CategoryId, categoryName = category.CategoryName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while creating the category: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            categoryName = categoryName.Trim();

            // Validate that category name contains only letters
            if (!System.Text.RegularExpressions.Regex.IsMatch(categoryName, @"^[a-zA-Z]+$"))
            {
                return Json(new { success = false, message = "Category name can only contain letters." });
            }

            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            if (await _context.Category.AnyAsync(c => c.CategoryName == categoryName && c.CategoryId != id))
            {
                return Json(new { success = false, message = "A category with this name already exists." });
            }

            try
            {
                category.CategoryName = categoryName;
                _context.Update(category);
                await _context.SaveChangesAsync();
                
                // Try to log, but don't fail if audit logging fails
                try
                {
                    await _auditLogService.LogActionAsync("Update", $"Category: {category.CategoryName}");
                }
                catch { /* Audit logging failed, but category was updated */ }
                
                return Json(new { success = true, message = "Category updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating the category: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found." });
            }

            try
            {
                _context.Category.Remove(category);
                await _context.SaveChangesAsync();
                
                // Try to log, but don't fail if audit logging fails
                try
                {
                    await _auditLogService.LogActionAsync("Delete", $"Category: {category.CategoryName}");
                }
                catch { /* Audit logging failed, but category was deleted */ }
                
                return Json(new { success = true, message = "Category deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the category: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Category.ToListAsync();
            return Json(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Inventory
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
                    p.Description
                })
                .ToListAsync();
            return Json(products);
        }

        private bool ProductsExists(int id)
        {
            return (_context.Inventory?.Any(e => e.ProductId == id)).GetValueOrDefault();
        }

        private async Task<string> SaveProductImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return string.Empty;
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(imageFile.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }
    }
}
