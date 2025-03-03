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
            var leoContext = _context.Users.Include(p => p.Roles);
            return View(await leoContext.ToListAsync());
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FirstName,LastName,Email,Username,Password,RoleId,IsAdmin")] Users users)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate entries
                if (await IsDuplicateUser(users))
                {
                    ModelState.AddModelError("", "User with the same FirstName, LastName, Email, or Username already exists.");
                    ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName");
                    return View(users);
                }

                // Hash the password before saving
                string hashedPassword = HashingServices.HashData(users.Password);
                users.Password = hashedPassword;

                _context.Add(users);
                await _context.SaveChangesAsync();

                // Set TempData for login success
                TempData["LoginSuccess"] = "Added successfully";
                return RedirectToAction(nameof(Create));
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
        // POST: Users/Edit/5
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
                    ModelState.AddModelError("", "User with the same FirstName, LastName, Email, or Username already exists.");
                    ViewData["RoleId"] = new SelectList(_context.Set<Role>(), "RoleId", "RoleName"); // Add this line
                    return View(users);
                }

                try
                {
                    // Hash the password before saving
                    string hashedPassword = HashingServices.HashData(users.Password);
                    users.Password = hashedPassword;

                    _context.Update(users);

                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Updated successfully";
                    await _context.SaveChangesAsync();
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

            // This line ensures that the RoleId is set even if the model state is not valid
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
