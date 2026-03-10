using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Required for Security
using MySuperPOS.Data;
using MySuperPOS.Models;
using MySuperPOS.Mappers; 
using CsvHelper;
using IronBarCode; 

namespace MySuperPOS.Controllers
{
    // [Authorize] ensures only logged-in users can access ANY part of the inventory
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.OrderBy(p => p.Name).ToListAsync());
        }

        // --- BARCODE GENERATION ---
        // Accessible only to logged-in staff
        public IActionResult Barcode(int id)
        {
            try
            {
                string data = id.ToString().PadLeft(8, '0');
                var myBarcode = BarcodeWriter.CreateBarcode(data, BarcodeWriterEncoding.Code128);
                myBarcode.ResizeTo(200, 80);
                
                byte[] binaryData = myBarcode.ToPngBinaryData();
                return File(binaryData, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest("Barcode Error: " + ex.Message);
            }
        }

        public async Task<IActionResult> PrintBarcodes()
        {
            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        // --- INVENTORY UPDATES ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int id, int addedQuantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null && addedQuantity > 0)
            {
                product.StockQuantity += addedQuantity;
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Stock Updated: Added {addedQuantity} units to {product.Name}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid CSV file.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                csv.Context.RegisterClassMap<ProductUploadMap>();
                var records = csv.GetRecords<Product>().ToList();
                
                foreach (var record in records)
                {
                    var existingProduct = await _context.Products
                        .FirstOrDefaultAsync(p => p.Name.ToLower() == record.Name.ToLower());

                    if (existingProduct != null)
                    {
                        existingProduct.StockQuantity += record.StockQuantity;
                        _context.Update(existingProduct);
                    }
                    else
                    {
                        _context.Products.Add(record);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Bulk inventory update successful.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "CSV Error: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // --- STANDARD CRUD ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            return product == null ? NotFound() : View(product);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,StockQuantity,Category")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            return product == null ? NotFound() : View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,StockQuantity,Category")] Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            return product == null ? NotFound() : View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id) => _context.Products.Any(e => e.Id == id);
    }
}