using System.ComponentModel.DataAnnotations;

namespace leo.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }


        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "The category name can only contain letters.")]
        public string CategoryName { get; set; }

        // Navigation property for related Products
        public ICollection<Inventory>? Product { get; set; }

    }
}
