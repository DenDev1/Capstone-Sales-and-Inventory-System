using leo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

[Authorize]
public class AuditLogsController : Controller
{
    private readonly leoContext _context;
 
    public AuditLogsController(leoContext context)
    {
        _context = context;
    }

    // GET: AuditLogs
    public async Task<IActionResult> Index()
    {
        var auditLogs = await _context.AuditLogs
            .Include(a => a.User) // Include the User navigation property
            .ToListAsync();

        return View(auditLogs);
    }

    [HttpPost]
    public async Task<IActionResult> ClearHistory()
    {
        // Assuming _context is your DbContext
        var logs = await _context.AuditLogs.ToListAsync(); // Get all logs
        _context.AuditLogs.RemoveRange(logs); // Remove all logs
        await _context.SaveChangesAsync(); // Save changes to the database


        // Set TempData for login success
        TempData["LoginSuccess"] = "All audit logs have been cleared.";
   

        return RedirectToAction(nameof(Index)); // Redirect to the index or appropriate view
    }


    // GET: AuditLogs/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(m => m.LogId == id);

        if (auditLog == null)
        {
            return NotFound();
        }

        return View(auditLog);
    }

    // POST: AuditLogs/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var auditLog = await _context.AuditLogs.FindAsync(id);
        if (auditLog != null)
        {
            _context.AuditLogs.Remove(auditLog);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
