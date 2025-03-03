using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using leo.Data;
using leo.Models;
using Microsoft.AspNetCore.Authorization;

namespace leo.Controllers
{
    [Authorize] // Uncomment this line to restrict access to admin role only
    public class CategoriesController : Controller
    {
        private readonly leoContext _context;
        private readonly AuditLogService _auditLogService;

        public CategoriesController(leoContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService; // Inject AuditLogService
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            return _context.Category != null ?
                   View(await _context.Category.ToListAsync()) :
                   Problem("Entity set 'leoContext.Category' is null.");
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Category == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate category name
                if (await _context.Category.AnyAsync(c => c.CategoryName == category.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();

                // Set TempData for login success
                TempData["LoginSuccess"] = "Added successfully";
                // Log the creation action
                await _auditLogService.LogActionAsync("Create", $"Category: {category.CategoryName}");

                return RedirectToAction(nameof(Create));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Category == null)
            {
                return NotFound();
            }

            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate category name
                    if (await _context.Category.AnyAsync(c => c.CategoryName == category.CategoryName && c.CategoryId != id))
                    {
                        ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    // Set success message
                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Updated successfully";
                    // Log the edit action
                    await _auditLogService.LogActionAsync("Updated", $"Category: {category.CategoryName}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
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
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Category == null)
            {
                return Problem("Entity set 'leoContext.Category' is null.");
            }

            // Find the category by id
            var category = await _context.Category.FindAsync(id);

            // If no category is found, return NotFound
            if (category == null)
            {
                return NotFound();
            }

            // Remove the category
            _context.Category.Remove(category);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Set a success message in TempData to show after redirect
            // Set TempData for login success
            TempData["LoginSuccess"] = "Deleted successfully";

            // Optionally, log the delete action
            await _auditLogService.LogActionAsync("Delete ", $"Category: {category.CategoryName}");

            // Redirect to the Index action (list of categories) after deletion
            return RedirectToAction(nameof(Index));
        }


        private bool CategoryExists(int id)
        {
            return (_context.Category?.Any(e => e.CategoryId == id)).GetValueOrDefault();
        }
    }
}
