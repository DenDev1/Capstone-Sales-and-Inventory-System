using leo.Data;
using leo.Models;
using Microsoft.AspNetCore.Http; // Ensure you have the correct namespace for IHttpContextAccessor
using System.Security.Claims;

public class AuditLogService
{
    private readonly leoContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(leoContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task LogActionAsync(string action, string description)
    {
        var user = _httpContextAccessor.HttpContext.User;

        // Retrieve the email from claims
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com"; // Use ClaimTypes.Email

        // Create the log entry
        var log = new AuditLog
        {
            Action = action,
            Description = description,
            DateOfAction = DateTime.Now,
            Email = email, // Ensure Email is set here
            UserRole = user.IsInRole("Admin") ? "Admin" : "Staff" // Determine role
        };

        // Add the log entry to the database
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }



}





