using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using leo.Models;
using leo.Services;
using leo.Data;

namespace leo.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly leoContext _context;

        public UsersController(leoContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            var leoContext = _context.Users.Include(p => p.Roles);
            return View(await leoContext.ToListAsync());
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FirstName,LastName,Email,Username,Password,RoleId,IsAdmin")] Users users)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate entries
                if (await IsDuplicateUser(users))
                {
                    string errorMsg = "User with the same Identity, Email, or Username already exists.";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = errorMsg });
                    }
                    ModelState.AddModelError("", errorMsg);
                    ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
                    return View(users);
                }

                // Hash the password before saving
                string hashedPassword = HashingServices.HashData(users.Password);
                users.Password = hashedPassword;

                _context.Add(users);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Staff member added successfully!" });
                }

                TempData["LoginSuccess"] = "Added successfully";
                return RedirectToAction(nameof(Create));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(" ", errors) });
            }

            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            return View(users);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var users = await _context.Users.FindAsync(id);
            if (users == null)
            {
                return NotFound();
            }

            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            return View(users);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FirstName,LastName,Email,Username,Password,RoleId,IsAdmin")] Users users)
        {
            if (id != users.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check for duplicate entries, excluding the current user being edited
                if (await IsDuplicateUser(users, id))
                {
                    string errorMsg = "User with same details already exists.";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = errorMsg });
                    }
                    ModelState.AddModelError("", errorMsg);
                    ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
                    return View(users);
                }

                try
                {
                    // Hash the password if changed
                    if (!string.IsNullOrEmpty(users.Password))
                    {
                        string hashedPassword = HashingServices.HashData(users.Password);
                        users.Password = hashedPassword;
                    }
                    else
                    {
                        // If password is empty, keep the original one
                        _context.Entry(users).Property(x => x.Password).IsModified = false;
                    }

                    _context.Update(users);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Staff details updated!" });
                    }

                    TempData["LoginSuccess"] = "Updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsersExists(users.UserId))
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

            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            return View(users);
        }


        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'leoContext.Users' is null.");
            }

            var users = await _context.Users.FindAsync(id);
            if (users != null)
            {
                _context.Users.Remove(users);
                // Hash the password before saving (optional, as you may not need the password for delete)
                string hashedPassword = HashingServices.HashData(users.Password);
                users.Password = hashedPassword;

                // Set TempData for login success
                TempData["LoginSuccess"] = "Deleted successfully";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int? id)
        {
            if (id == null) return BadRequest();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Json(new {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                username = user.Username,
                roleId = user.RoleId
            });
        }

        private bool UsersExists(int id)
        {
            return (_context.Users?.Any(e => e.UserId == id)).GetValueOrDefault();
        }

        private async Task<bool> IsDuplicateUser(Users users, int? excludeId = null)
        {
            return await _context.Users.AnyAsync(u =>
                (excludeId == null || u.UserId != excludeId) &&
                (u.FirstName == users.FirstName || u.LastName == users.LastName ||
                 u.Email == users.Email || u.Username == users.Username));
        }
    }
}
