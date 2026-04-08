
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace leo.Models
{
    public class Inventory
    {
        [Key]
        public int ProductId { get; set; }


        [Required(ErrorMessage = "Product name is required.")]
        [RegularExpression(@"^[0-9A-Za-z\s\-.]+$", ErrorMessage = "The product name can only contain letters, numbers, spaces, hyphens, and periods.")]
        public string ProductName { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9A-Za-z\-]+$", ErrorMessage = "Invalid barcode format.")]
        public string Barcode { get; set; } = string.Empty;

        public string Suppliers { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime Date { get; set; }
        // UnitPrice should only be a valid decimal number
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be a positive number.")]
        public decimal UnitPrice { get; set; }

        // StockQuantity should only be a valid integer number
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be a non-negative integer.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Image path cannot exceed 255 characters.")]
        public string ImagePath { get; set; } = string.Empty; // Path to uploaded product image (e.g., /uploads/products/123.jpg)

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
