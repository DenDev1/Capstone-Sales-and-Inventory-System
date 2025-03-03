using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using leo.Data;
using leo.Models;
using Microsoft.AspNetCore.Authorization;

namespace leo.Controllers
{
    [Authorize]
    public class TransactionHistoriesController : Controller
    {
        private readonly leoContext _context;

        public TransactionHistoriesController(leoContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> TransactionIndex()
        {
            var transactions = await _context.TransactionHistory
          .Include(t => t.Supplier)

          .ToListAsync();

            return View(transactions);
        }



        // Action to clear all transaction history (now in SuppliersController)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearHistory()
        {
            try
            {
                var transactionHistories = await _context.TransactionHistory.ToListAsync();

                if (transactionHistories.Any())
                {
                    _context.TransactionHistory.RemoveRange(transactionHistories);
                    await _context.SaveChangesAsync();

                    // Set TempData for login success
                    TempData["LoginSuccess"] = "Cleared successfully";


                }
                else
                {
                    TempData["HistoryCleared"] = "No transaction history to clear.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while clearing history: {ex.Message}";
            }

            // Redirect to the TransactionIndex action of TransactionHistoriesController
            return RedirectToAction("TransactionIndex");
        }



    }
}