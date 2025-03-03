using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; } // Unique ID for the audit log

        public string Action { get; set; } // The action performed (e.g., Stock Change, Price Adjustment)



        public DateTime DateOfAction { get; set; } = DateTime.Now; // Date when the action was performed

        public string Description { get; set; } // Description of the action
        public int? UserId { get; set; }
        public  Users? User { get; set; } // Navigation property
        public string UserRole { get; set; } // Add this property
        public string Email { get; set; } // Change Username to Email
    }

}
