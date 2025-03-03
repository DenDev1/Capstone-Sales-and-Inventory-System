namespace leo.ViewModels
{
    public class DailySalesViewModel
    {
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
    }

}
