using System.ComponentModel.DataAnnotations;

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

        // New property for partial payment amount
        public decimal PartialPaymentAmount { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;


        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Customer name must contain only letters.")]
        public string CustomerName { get; set; } // Add this property

        // Computed property to calculate remaining balance after partial payment
        public decimal RemainingBalance => TotalAmount - PartialPaymentAmount;

 
        // public string ReferenceNo { get; set; } // Add this property
    }

    public enum PaymentStatus
    {
        Cash,
        Partial,
        FullyPaid
    }
}
