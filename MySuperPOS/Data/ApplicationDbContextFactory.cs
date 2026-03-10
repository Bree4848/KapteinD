using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MySuperPOS.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Hardcoding the string here tells the tool EXACTLY where to go 
            // without searching through appsettings.json
            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=MySuperPOSDB;Trusted_Connection=True;MultipleActiveResultSets=true";

            builder.UseSqlServer(connectionString);

            return new ApplicationDbContext(builder.Options);
        }
    }
}