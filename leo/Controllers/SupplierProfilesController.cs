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

namespace leo.Controllers
{
    [Authorize]
    public class SupplierProfilesController : Controller
    {
        private readonly leoContext _context;
        //private readonly AuditLogService _auditLogService; // Injecting AuditLogService
        public SupplierProfilesController(leoContext context)
        {
            _context = context;
        }

        // GET: SupplierProfiles
        public async Task<IActionResult> Index()
        {
            return _context.SupplierProfile != null ?
                        View(await _context.SupplierProfile.ToListAsync()) :
                        Problem("Entity set 'leoContext.SupplierProfile'  is null.");
        }


        // GET: SupplierProfiles/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProfileId,CompanyName,PhoneNumber,Address,Supplier,ContactEmail")] SupplierProfile supplierProfile)
        {
            if (ModelState.IsValid)
            {
                // Check if there is already a supplier profile with the same ProfileName or ContactEmail
                var existingProfile = await _context.SupplierProfile
                    .FirstOrDefaultAsync(p => p.CompanyName == supplierProfile.CompanyName || p.ContactEmail == supplierProfile.ContactEmail);

                if (existingProfile != null)
                {
                    ModelState.AddModelError("", "A supplier with the same Profile Name already exists.");
                    return View(supplierProfile);
                }

                _context.Add(supplierProfile);
                await _context.SaveChangesAsync();
                // Log the creation action
                //await _auditLogService.LogActionAsync("Added", $"Supplier Profile: {supplierProfile.ProfileName}");

                // Set TempData for success message
                TempData["LoginSuccess"] = "Added successfully";

                return RedirectToAction(nameof(Create));
            }
            return View(supplierProfile);
        }

        // GET: SupplierProfiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.SupplierProfile == null)
            {
                return NotFound();
            }

            var supplierProfile = await _context.SupplierProfile.FindAsync(id);
            if (supplierProfile == null)
            {
                return NotFound();
            }
            return View(supplierProfile);
        }

        // POST: SupplierProfiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProfileId,CompanyName,PhoneNumber,Address,Supplier,ContactEmail")] SupplierProfile supplierProfile)
        {
            if (id != supplierProfile.ProfileId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check for duplicate ProfileName or ContactEmail, excluding the current profile being edited
                var existingProfile = await _context.SupplierProfile
                    .FirstOrDefaultAsync(p => (p.CompanyName == supplierProfile.CompanyName || p.ContactEmail == supplierProfile.ContactEmail)
                                              && p.ProfileId != supplierProfile.ProfileId);

                if (existingProfile != null)
                {
                    ModelState.AddModelError("", "A supplier with the same Profile Name or Contact Email already exists.");
                    return View(supplierProfile);
                }

                try
                {
                    _context.Update(supplierProfile);
                    await _context.SaveChangesAsync();
                    // Log the creation action
                    //await _auditLogService.LogActionAsync("Updated", $"Supplier Profile: {supplierProfile.ProfileName}");
                    // Set TempData for success message
                    TempData["LoginSuccess"] = "Updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierProfileExists(supplierProfile.ProfileId))
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
            return View(supplierProfile);
        }




        // POST: SupplierProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.SupplierProfile == null)
            {
                return Problem("Entity set 'leoContext.SupplierProfile'  is null.");
            }
            var supplierProfile = await _context.SupplierProfile.FindAsync(id);
            if (supplierProfile != null)
            {
                _context.SupplierProfile.Remove(supplierProfile);
            }

            await _context.SaveChangesAsync();
            //// Log the creation action
            //await _auditLogService.LogActionAsync("Deleted", $"Supplier Profile: {supplierProfile.ProfileName}");
            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private bool SupplierProfileExists(int id)
        {
            return (_context.SupplierProfile?.Any(e => e.ProfileId == id)).GetValueOrDefault();
        }
    }
}
