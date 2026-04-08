using System.ComponentModel.DataAnnotations;

namespace leo.ViewModels
{
    public class PosCheckoutRequest
    {
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<PosCheckoutItemRequest> Items { get; set; } = new();
    }

    public class PosCheckoutItemRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
