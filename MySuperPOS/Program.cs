using Microsoft.EntityFrameworkCore;
using MySuperPOS.Data;
using Microsoft.AspNetCore.Identity; // Added for Identity

var builder = WebApplication.CreateBuilder(args);

// --- ADD SERVICES HERE ---
builder.Services.AddControllersWithViews();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. Add Identity Services
// This configures how passwords work and links Identity to your database
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.Password.RequireDigit = false; 
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false; // Set to true if you want email verification
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// 2. Required for Identity's built-in Razor Pages (Login/Register)
builder.Services.AddRazorPages();

var app = builder.Build();

// --- CONFIGURE PIPELINE HERE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// 3. Add Authentication BEFORE Authorization
app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

// 4. Map both Controllers and Razor Pages (for the login screens)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); 

app.Run();