using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace leo.ViewModels
{
    public class SupplierViewModel
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Supplier Name name must contain only letters.")]
        public string SupplierName { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "ProductName name must contain only letters.")]
        public string ProductName { get; set; } = string.Empty;

        public string? ProductsAndQuantities { get; set; }
        public List<SupplierLineItemViewModel> LineItems { get; set; } = new();
        public List<string> Products { get; set; } = new();
        public List<int> Quantities { get; set; } = new();
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 1.")]
        public int Quantity { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "9999999999", ErrorMessage = "Unit price must be greater than 0.")]
        public decimal UnitPrice { get; set; }

        public string? Status { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        public DateTime ProductDate { get; set; } = DateTime.Now;
    }

    public sealed class SupplierLineItemViewModel
    {
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public sealed class SupplierCreateViewModel : IValidatableObject
    {
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Supplier Name name must contain only letters.")]
        public string SupplierName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }
        
        [Range(typeof(decimal), "0.01", "9999999999", ErrorMessage = "Unit price must be greater than 0.")]
        public decimal UnitPrice { get; set; }
        public List<SupplierLineItemViewModel> LineItems { get; set; } = new();
        public string? ProductsAndQuantities { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (LineItems == null || LineItems.Count == 0)
            {
                yield return new ValidationResult(
                    "Please add at least one product and quantity.",
                    new[] { nameof(LineItems) });
                yield break;
            }

            if (LineItems.Count > 50)
            {
                yield return new ValidationResult(
                    "Too many requested items. Please keep it to 50 or less.",
                    new[] { nameof(LineItems) });
            }

            var hasBlank = LineItems.Any(i => i == null || string.IsNullOrWhiteSpace(i.ProductName));
            if (hasBlank)
            {
                yield return new ValidationResult(
                    "Each requested item must have a product name.",
                    new[] { nameof(LineItems) });
            }

            var hasInvalidQty = LineItems.Any(i => i != null && i.Quantity < 1);
            if (hasInvalidQty)
            {
                yield return new ValidationResult(
                    "Each requested item must have a quantity of at least 1.",
                    new[] { nameof(LineItems) });
            }
        }
    }
}
