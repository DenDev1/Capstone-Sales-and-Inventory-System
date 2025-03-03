using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class TransactionHistory 
    {
        [Key]
        public int NewTransactionId { get; set; } // Auto-increment ID
        public DateTime Date { get; set; } // Date of transaction
        public decimal Amount { get; set; } // Transaction amount
        public string ProductType { get; set; } // Type of product
        public DateTime TransactionDate { get; set; } // Date of transaction
        public string TransactionType { get; set; } // Type of transaction (e.g., created, updated)
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public string? ProductsAndQuantities { get; set; }  // Nullable string

        public int? SupplierId { get; set; }
        public Supplier Supplier { get; set; } // Navigation property
    }


}


