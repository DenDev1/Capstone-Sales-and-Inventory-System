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
using leo.Services;

namespace leo.Controllers
{
    [Authorize]
    public class ReturnsController : Controller
    {
        private readonly leoContext _context;
        private readonly ReturnService _returnService;
        private readonly AuditLogService _auditLogService; // Injecting AuditLogService

        public ReturnsController(leoContext context, ReturnService returnService, AuditLogService auditLogService)
        {
            _context = context;
            _returnService = returnService; // Assign the ReturnService
            _auditLogService = auditLogService;
        }

        [Authorize(Roles = "Staff")] // Only Staff can access this action
        public IActionResult Create()
        {
            ViewBag.ProductId = new SelectList(_context.Inventory, "ProductId", "ProductName");
            return View();
        }

        // Reject return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                await RejectReturnAsync(id); // Call the reject method

                // Set TempData for login success
                TempData["LoginSuccess"] = "Rejected successfully";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
            }

            return RedirectToAction(nameof(Index)); // Redirect back to the index view
        }

        private async Task RejectReturnAsync(int returnId)
        {
            // Find the return item including the associated product
            var returnItem = await _context.Return
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId);

            // Check if return item exists and is in the UnderWarranty status
            if (returnItem != null && returnItem.Status == ReturnStatus.UnderWarranty)
            {
                // Logic for rejecting the return
                returnItem.Status = ReturnStatus.Rejected; // Set status to 'Rejected'

                // Update stock quantity
                if (returnItem.Product != null)
                {
                    returnItem.Product.StockQuantity += returnItem.QuantityReturned; // Add back the returned quantity

                    // Ensure stock quantity does not go below zero
                    if (returnItem.Product.StockQuantity < 0)
                    {
                        returnItem.Product.StockQuantity = 0;
                    }
                }

                await _context.SaveChangesAsync(); // Save the changes to the database
            }
            else
            {
                // Handle case when return item is not found or is not valid for rejection
                throw new InvalidOperationException("Return item not found or not in a valid state for rejection.");
            }
        }

        // Approve return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var returnItem = await _context.Return.Include(r => r.Product).FirstOrDefaultAsync(r => r.ReturnId == id);
            if (returnItem == null || returnItem.Status != ReturnStatus.UnderWarranty)
            {
                return NotFound(); // Or return an error message if not found
            }

            // Update stock if the status is Approved
            returnItem.Product.StockQuantity += returnItem.QuantityReturned;
            returnItem.Status = ReturnStatus.Approved; // Set status to 'Approved'

            await _context.SaveChangesAsync(); // Save changes to the database


            // Set TempData for login success
            TempData["LoginSuccess"] = "Approved successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Returns
        public async Task<IActionResult> Index()
        {
            var leoContext = _context.Return.Include(a => a.Product);
            return View(await leoContext.ToListAsync());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff")] // Only Staff can access this action
        public async Task<IActionResult> Create(Return newReturn)
        {
            // Step 1: Check if the newReturn object is null
            if (newReturn == null)
            {
                ModelState.AddModelError(string.Empty, "Return details cannot be null.");
                return View(newReturn); // Return the view with the model to show validation errors
            }

            // Step 2: Trim any whitespace in string properties (if applicable)
            // Assuming you have other string properties in the Return model
            // newReturn.OtherPropertyName = newReturn.OtherPropertyName?.Trim();

            // Step 3: Check if a return with the same ProductId already exists
            //bool productExists = _context.Return.Any(r => r.ProductId == newReturn.ProductId);
            //if (productExists)
            //{
            //    ModelState.AddModelError(nameof(newReturn.ProductId), "A return for this product already exists.");
            //    return View(newReturn); // Return the view with the model to show validation errors
            //}

            // Step 4: Get all existing returns for the current date
            var existingReturns = _context.Return
                .Where(r => r.OrderDate.Date == DateTime.Now)
                .ToList();

            // Step 5: Set OrderDate
            if (existingReturns.Any())
            {
                // If there are existing returns today, set OrderDate to the next day
                newReturn.OrderDate = DateTime.Now.AddDays(1);
            }
            else
            {
                // If no existing returns, set it to today's date
                newReturn.OrderDate = DateTime.Now;
            }

            // Step 6: Check if beyond 30 days
            if ((DateTime.Today - newReturn.OrderDate).Days > 30)
            {
                // Automatically set status to Approved
                newReturn.Status = ReturnStatus.Approved;
            }

            // Step 7: Validate model state before saving
            if (!ModelState.IsValid)
            {
                return View(newReturn); // Return the view with the model to show validation errors
            }

            // Step 8: Save the return to the database
            _context.Add(newReturn);
            await _context.SaveChangesAsync();

            // Step 9: Log the order creation action to AuditLogService
            await _auditLogService.LogActionAsync("Added", $"Return {newReturn.ProductId}");

            // Set TempData for login success
            TempData["LoginSuccess"] = "Added successfully";
            return RedirectToAction(nameof(Create));
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Return == null)
            {
                return NotFound();
            }

            var @return = await _context.Return.FindAsync(id);
            if (@return == null)
            {
                return NotFound();
            }

            ViewData["ProductId"] = new SelectList(_context.Inventory, "ProductId", "ProductName", @return.ProductId);
            return View(@return);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReturnId,ProductId,QuantityReturned,Reason,Status,OrderDate,ReturnDate")] Return @return)
        {
            if (id != @return.ReturnId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if OrderDate is more than 30 days old
                    if ((DateTime.Now - @return.OrderDate).TotalDays > 30)
                    {
                        @return.Status = ReturnStatus.Approved; // Automatically approve if older than 30 days
                    }

                    @return.ReturnDate = DateTime.Now.AddDays(1); // Set ReturnDate to tomorrow

                    _context.Update(@return);
                    await _context.SaveChangesAsync();

                    // Step 9: Log the order edit action to AuditLogService
                    await _auditLogService.LogActionAsync("Updated", $"Return:{@return.ProductId}");


                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReturnExists(@return.ReturnId))
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

            ViewData["ProductId"] = new SelectList(_context.Inventory, "ProductId", "ProductName", @return.ProductId);
            return View(@return);
        }

        // POST: Returns/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Return == null)
            {
                return Problem("Entity set 'leoContext.Return' is null.");
            }

            // Find the return record by id
            var returnRecord = await _context.Return.FindAsync(id);

            // If no return record is found, return NotFound
            if (returnRecord == null)
            {
                return NotFound();
            }

            // Remove the return record
            _context.Return.Remove(returnRecord);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Set a success message in TempData to show after redirect

            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";

            // Step 9: Log the order creation action to AuditLogService
            await _auditLogService.LogActionAsync("Deleted", $"Return {returnRecord.ProductId}");
            // Redirect to the Index action (list of returns) after deletion
            return RedirectToAction(nameof(Index));
        }

        private bool ReturnExists(int id)
        {
            return (_context.Return?.Any(e => e.ReturnId == id)).GetValueOrDefault();
        }
    }
}
