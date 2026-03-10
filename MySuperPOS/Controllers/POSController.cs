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
        // Main checkout interface with product grid and category filters
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Extract unique categories for the UI filter buttons
            ViewBag.Categories = products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(products);
        }

        // --- TRANSACTION ENGINE ---

        // POST: POS/CompleteSale
        // Processes the cart, deducts inventory, and saves the transaction
        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] Sale sale)
        {
            if (sale == null || !sale.SaleItems.Any()) 
            {
                return BadRequest("Transaction failed: Cart is empty.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                sale.SaleDate = DateTime.Now;
                _context.Sales.Add(sale);

                foreach (var item in sale.SaleItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null)
                        return BadRequest($"Product ID {item.ProductId} not found.");

                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}");

                    // Atomically deduct stock
                    product.StockQuantity -= item.Quantity;
                    _context.Update(product);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Return the Sale ID for receipt redirection
                return Ok(new { success = true, saleId = sale.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        // --- PRINTING & VIEWING ---

        // GET: POS/Receipt/5
        // Generates a clean, printable thermal-style receipt
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();

            return View(sale);
        }

        // --- MANAGEMENT & ACCOUNTING ---

        // GET: POS/DailyReport
        // List of all sales made today for record-keeping
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

        // GET: POS/CashUp
        // Reconciliation tool to balance the cash drawer at the end of a shift
        public async Task<IActionResult> CashUp()
        {
            var today = DateTime.Today;
            
            var expectedTotal = await _context.Sales
                .Where(s => s.SaleDate >= today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var transactionCount = await _context.Sales
                .CountAsync(s => s.SaleDate >= today);

            ViewBag.Expected = expectedTotal;
            ViewBag.Count = transactionCount;

            return View();
        }
    }
}