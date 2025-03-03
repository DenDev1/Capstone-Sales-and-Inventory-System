using leo.Data;
using leo.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace leo.Services
{
    public class ReturnService
    {
        private readonly leoContext _context;

        public ReturnService(leoContext context)
        {
            _context = context;
        }



        // Reject return (Marks the return as rejected and adjusts stock)
        public void RejectReturn(int returnId)
        {
            // Fetch the return item and include related product
            var returnItem = _context.Return.Include(r => r.Product).FirstOrDefault(r => r.ReturnId == returnId);

            if (returnItem == null)
            {
                throw new ArgumentException($"Return with ID {returnId} not found.");
            }

            if (returnItem.Status != ReturnStatus.UnderWarranty)
            {
                throw new InvalidOperationException("Only pending returns can be rejected.");
            }

            // Ensure the product associated with the return item exists
            if (returnItem.Product == null)
            {
                throw new InvalidOperationException("Product associated with the return not found.");
            }

            // Logic to mark the return as rejected
            returnItem.Status = ReturnStatus.Rejected;

            // Remove the returned quantity from the product's stock
            returnItem.Product.StockQuantity -= returnItem.QuantityReturned;

            // Ensure stock quantity does not go below zero
            if (returnItem.Product.StockQuantity < 0)
            {
                returnItem.Product.StockQuantity = 0; // Prevent negative stock
            }

            // Save changes to the database
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to update the database: " + ex.Message);
            }
        }

        // Create a new return (validates and adds a return to the database)
        public void CreateReturn(Return returnItem)
        {
            if (returnItem == null)
            {
                throw new ArgumentNullException(nameof(returnItem), "Return item cannot be null.");
            }

            if (returnItem.QuantityReturned <= 0)
            {
                throw new ArgumentException("Quantity returned must be greater than zero.");
            }

            // Add the return item to the database
            _context.Return.Add(returnItem);
            _context.SaveChanges();
        }
    }
}
