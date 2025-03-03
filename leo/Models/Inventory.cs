
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace leo.Models
{
    public class Inventory
    {
        [Key]
        public int ProductId { get; set; }


        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "The product name can only contain letters.")]
        public string ProductName { get; set; }


        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [ForeignKey("SupplierProfile")] // Ensure this is correct if using Data Annotations
        public int ProfileId { get; set; }	

        public  SupplierProfile? SupplierProfile { get; set; }


        public DateTime Date { get; set; }
        // UnitPrice should only be a valid decimal number
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be a positive number.")]
        public decimal UnitPrice { get; set; }

        // StockQuantity should only be a valid integer number
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer.")]
        public int StockQuantity { get; set; }

        public string Description { get; set; }

        public bool IsDeleted { get; set; } = false; // Flag for soft delete


        public ICollection<Order> Orders { get; set; } = new List<Order>();
        // Add stock status
        [Required(ErrorMessage = "Stock status is required.")]
        public StockStatus StockStatus { get; set; }



    }
}



public enum StockStatus
{
    InStock = 0,   // 0
    LowStock = 1,  // 1
    OutOfStock = 2 // 2
}
