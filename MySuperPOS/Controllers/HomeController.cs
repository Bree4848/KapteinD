using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySuperPOS.Data;
using MySuperPOS.Models;

namespace MySuperPOS.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    // Injecting the Database Context and Logger
    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-6);

            // 1. Calculate Today's Total Revenue
            // We use (decimal?) to prevent errors if there are zero sales recorded
            var salesToday = await _context.Sales
                .Where(s => s.SaleDate >= today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            // 2. Count Total Transactions for Today
            var transactionCount = await _context.Sales
                .CountAsync(s => s.SaleDate >= today);

            // 3. Identify Low Stock Items (Threshold: less than 10 units)
            var lowStockItems = await _context.Products
                .Where(p => p.StockQuantity < 10)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            // 4. Prepare Chart Data: Last 7 Days of Sales
            // This groups sales by date to create a timeline of revenue
            var weeklySales = await _context.Sales
                .Where(s => s.SaleDate >= sevenDaysAgo)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    Total = g.Sum(s => s.TotalAmount) 
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // Pass data to ViewBag for the View
            ViewBag.DailyTotal = salesToday;
            ViewBag.TransactionCount = transactionCount;
            ViewBag.LowStock = lowStockItems;

            // Map the chart data to separate lists for JavaScript labels and values
            ViewBag.ChartLabels = weeklySales.Select(s => s.Date.ToString("dd MMM")).ToList();
            ViewBag.ChartData = weeklySales.Select(s => s.Total).ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading dashboard statistics.");
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}