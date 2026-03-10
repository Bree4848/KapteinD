using System.Collections.Generic;
using System;

namespace MySuperPOS.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        
        // Navigation property for the items in this sale
        public List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation properties
        public Product? Product { get; set; }
    }
}