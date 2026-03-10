using Microsoft.EntityFrameworkCore;
using MySuperPOS.Data;

var builder = WebApplication.CreateBuilder(args);

// --- ADD SERVICES HERE (Before builder.Build) ---
builder.Services.AddControllersWithViews();

// Move this line here:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- CONFIGURE PIPELINE HERE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();