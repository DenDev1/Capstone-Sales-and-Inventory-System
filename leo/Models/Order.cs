using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace leo.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Inventory? Product { get; set; } // Navigation property

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 1.")]
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
        public StockStatus StockStatus { get; set; }

        // Computed property for Subtotal
        public decimal Subtotal => Quantity * UnitPrice;

        public decimal TotalAmount { get; set; }

        // Kept in the model for UI/business logic compatibility, but the live DB may not have this column yet.
        [NotMapped]
        public decimal PartialPaymentAmount { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string ReferenceNo { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Customer name must contain only letters.")]
        public string CustomerName { get; set; } = string.Empty; // Add this property

        // Kept in the model for UI/business logic compatibility, but the live DB may not have this column yet.
        [NotMapped]
        public string Barcode { get; set; } = string.Empty;

        // Computed property to calculate remaining balance after partial payment
        [NotMapped]
        public decimal RemainingBalance => TotalAmount - PartialPaymentAmount;

    }

    public enum PaymentStatus
    {
        Cash,
        Partial,
        FullyPaid
    }
}
