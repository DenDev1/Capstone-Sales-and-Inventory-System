using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }


        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Supplier Name name must contain only letters.")]
        public string SupplierName { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Produc tName name must contain only letters.")]
        public string ProductsName { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 1.")]
        public int Quantity { get; set; }
        public string? ProductsAndQuantities { get; set; }  // Nullable string

        public string Description { get; set; }

        //public decimal Balance { get; set; }

        // Editable Status property without any dependency on Balance
        public string? Status { get; set; }


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        // New transaction history for the supplier
        public ICollection<TransactionHistory> TransactionHistories { get; set; }



    }
}
