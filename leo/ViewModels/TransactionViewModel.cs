using System;
using System.ComponentModel.DataAnnotations;

namespace leo.ViewModels
{
    public class TransactionViewModel
    {
        public int SupplierId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string ProductType { get; set; }
    }
}
