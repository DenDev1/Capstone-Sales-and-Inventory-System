using leo.Models;

namespace leo.ViewModels
{
    public class OrderDetailsViewModel
    {
        public IEnumerable<Order> Orders { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
