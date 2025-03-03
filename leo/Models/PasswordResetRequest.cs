using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class PasswordResetRequest
    {

        [Required(ErrorMessage = "The password is required.")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long and at most 20 characters long.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).+$", ErrorMessage = "The password must contain both letters and numbers.")]
        public string NewPassword { get; set; }

    }
}