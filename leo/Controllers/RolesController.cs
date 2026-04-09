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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleId,RoleName")] Role role)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate role name
                if (await RoleNameExists(role.RoleName))
                {
                    string errorMsg = "A role with this name already exists.";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = errorMsg });
                    }
                    ModelState.AddModelError("RoleName", errorMsg);
                    return View(role);
                }

                _context.Add(role);
                await _context.SaveChangesAsync();
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Role created successfully!" });
                }
                
                TempData["LoginSuccess"] = "Role created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
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
                        string errorMsg = "A role with this name already exists.";
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = errorMsg });
                        }
                        ModelState.AddModelError("RoleName", errorMsg);
                        return View(role);
                    }

                    _context.Update(role);
                    await _context.SaveChangesAsync();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Role updated successfully!" });
                    }

                    TempData["LoginSuccess"] = "Role updated successfully!";
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
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
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

        [HttpGet]
        public async Task<IActionResult> GetRoleDetails(int? id)
        {
            if (id == null) return BadRequest();
            var role = await _context.Role.FindAsync(id);
            if (role == null) return NotFound();
            return Json(new { roleId = role.RoleId, roleName = role.RoleName });
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
