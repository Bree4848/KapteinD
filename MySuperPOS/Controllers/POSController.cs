using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySuperPOS.Data;
using MySuperPOS.Models;

namespace MySuperPOS.Controllers
{
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;

        public POSController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- SALES TERMINAL ---

        // GET: POS/Index
        // The main checkout interface for the cashier
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(products);
        }

        // --- TRANSACTION PROCESSOR ---

        // POST: POS/CompleteSale
        // Handles the logic for saving a sale and updating inventory levels
        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] Sale sale)
        {
            if (sale == null || !sale.SaleItems.Any()) 
            {
                return BadRequest("The transaction cannot be processed because the cart is empty.");
            }

            // Database Transaction ensures that either ALL records save or NONE do.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Finalize Sale Metadata
                sale.SaleDate = DateTime.Now;
                _context.Sales.Add(sale);

                // 2. Loop through items to update inventory
                foreach (var item in sale.SaleItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null)
                    {
                        return BadRequest($"Product ID {item.ProductId} not found.");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        return BadRequest($"Insufficient stock for {product.Name}. (Available: {product.StockQuantity})");
                    }

                    // Deduct the quantity from the shop's stock
                    product.StockQuantity -= item.Quantity;
                    _context.Update(product);
                }

                // 3. Commit all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return the generated Sale ID so the frontend can redirect to the receipt
                return Ok(new 
                { 
                    success = true, 
                    saleId = sale.Id, 
                    total = sale.TotalAmount 
                });
            }
            catch (Exception ex)
            {
                // If anything fails (DB crash, etc), revert changes
                await transaction.RollbackAsync();
                return StatusCode(500, $"Critical Error: {ex.Message}");
            }
        }

        // --- PRINTING & VIEWING ---

        // GET: POS/Receipt/5
        // Generates the thermal-style receipt for a specific transaction
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) 
            {
                return NotFound();
            }

            return View(sale);
        }

        // --- MANAGEMENT REPORTS ---

        // GET: POS/DailyReport
        // Provides a summary of all business activities for today
        public async Task<IActionResult> DailyReport()
        {
            var today = DateTime.Today;
            
            var salesToday = await _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .Where(s => s.SaleDate >= today)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(salesToday);
        }
    }
}