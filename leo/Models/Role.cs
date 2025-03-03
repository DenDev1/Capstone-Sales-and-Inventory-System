
using leo.Models;
using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, ErrorMessage = "Role name must be between 2 and 50 characters", MinimumLength = 2)]
        public string RoleName { get; set; }


        // Navigation property to relate users to roles
        public ICollection<Users>? User { get; set; }
    }
}
