using leo.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace leo.ViewModels
{
    public class SupplierViewModel
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Supplier Name name must contain only letters.")]
        public string SupplierName { get; set; }

        public int ProductId { get; set; } // Thisssss should bind to the selected product

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "ProductName name must contain only letters.")]
        public string ProductName { get; set; }  // This will display the product name
                                                 // Ensure default value for ProductsAndQuantities if null
        public string? ProductsAndQuantities { get; set; }  // Nullable string
        public List<string> Products { get; set; } = new List<string>();
        public List<int> Quantities { get; set; } = new List<int>();
        public string Description { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 1.")]
        public int Quantity { get; set; }

        //public decimal Balance { get; set; }

        // Automatically calculated status based on specific conditions
        public string? Status { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        // New property for date

        public DateTime ProductDate { get; set; } = DateTime.Now;// Add this property

    }
}
