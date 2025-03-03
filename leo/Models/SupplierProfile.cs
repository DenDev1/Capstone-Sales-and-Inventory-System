using System.ComponentModel.DataAnnotations;

namespace leo.Models;
public class SupplierProfile
{
    [Key]
    public int ProfileId { get; set; }

    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Supplier name must contain only letters.")]
    public string Supplier { get; set; } // Ensure this is correct

    [Required]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "CompanyName name must contain only letters.")]
    public string CompanyName { get; set; }


    public string ContactEmail { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }


 

    // Navigation property for related Products
    public ICollection<Inventory>? Product { get; set; }
}


