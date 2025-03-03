using System;
using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class Return
    {
        [Key]
        public int ReturnId { get; set; }


        [Required]
        public int ProductId { get; set; }

        public Inventory? Product { get; set; }
        [Required]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Customer name must contain only letters.")]
        public string CustomerName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity returned must be greater than 0.")]
        public int QuantityReturned { get; set; }

        [Required(ErrorMessage = "Reason for return is required.")]

    
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Reason name must contain only letters.")]
        public string Reason { get; set; }

        public ReturnStatus Status { get; set; } = ReturnStatus.UnderWarranty; // Default to 'Under Warranty'

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime ReturnDate { get; set; } = DateTime.Now;

    }

    public enum ReturnStatus
    {
        UnderWarranty,
        Rejected,
        Approved
    }
}
