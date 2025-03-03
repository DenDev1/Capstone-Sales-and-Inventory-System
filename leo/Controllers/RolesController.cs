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
    [Authorize]
    public class RolesController : Controller
    {
        private readonly leoContext _context;

        public RolesController(leoContext context)
        {
            _context = context;
        }

        // GET: Roles
        public async Task<IActionResult> Index()
        {
            return _context.Role != null ?
                View(await _context.Role.ToListAsync()) :
                Problem("Entity set 'leoContext.Role' is null.");
        }

        // GET: Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Role == null)
            {
                return NotFound();
            }

            var role = await _context.Role.FirstOrDefaultAsync(m => m.RoleId == id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleId,RoleName")] Role role)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate role name
                if (await RoleNameExists(role.RoleName))
                {
                    ModelState.AddModelError("RoleName", "A role with this name already exists.");
                    return View(role);
                }

                _context.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Role == null)
            {
                return NotFound();
            }

            var role = await _context.Role.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }

        // POST: Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoleId,RoleName")] Role role)
        {
            if (id != role.RoleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate role name, excluding current role
                    if (await RoleNameExists(role.RoleName, id))
                    {
                        ModelState.AddModelError("RoleName", "A role with this name already exists.");
                        return View(role);
                    }

                    _context.Update(role);
                    await _context.SaveChangesAsync();
                    // Set success message
                    TempData["SuccessMessage"] = "Updated successfully!";

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleExists(role.RoleId))
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
            return View(role);
        }

    

        // POST: Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Role == null)
            {
                return Problem("Entity set 'leoContext.Role' is null.");
            }

            var role = await _context.Role.FindAsync(id);
            if (role != null)
            {
                _context.Role.Remove(role);
                await _context.SaveChangesAsync();
                // Set success message
                TempData["SuccessMessage"] = "Deleted successfully!";

            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoleExists(int id)
        {
            return (_context.Role?.Any(e => e.RoleId == id)).GetValueOrDefault();
        }

        // Check if a role with the same name already exists (with optional ID exclusion)
        private async Task<bool> RoleNameExists(string roleName, int? excludedRoleId = null)
        {
            return await _context.Role
                .AnyAsync(r => r.RoleName == roleName && (excludedRoleId == null || r.RoleId != excludedRoleId));
        }
    }
}
