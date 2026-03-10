using CsvHelper.Configuration;
using MySuperPOS.Models;

namespace MySuperPOS.Mappers
{
    public sealed class ProductUploadMap : ClassMap<Product>
    {
        public ProductUploadMap()
        {
            // Map the CSV Header to the Model Property
            Map(m => m.Name).Name("Name");
            Map(m => m.Category).Name("Category");
            Map(m => m.Price).Name("Price");
            Map(m => m.StockQuantity).Name("StockQuantity");
            Map(m => m.Description).Name("Description").Optional(); // Optional in case CSV doesn't have it
        }
    }
}