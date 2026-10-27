using FitTrack.Data;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDbContext") ?? throw new InvalidOperationException("Connection string 'AppDbContext' not found.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("OpenAI", client => 
{
    client.BaseAddress = new Uri("https://api.openai.com");
    client.Timeout = TimeSpan.FromSeconds(90);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment())
    {
        var adminPassword = builder.Configuration["AdminAccount:Password"];
        if (!string.IsNullOrWhiteSpace(adminPassword))
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            const string adminRole = "Admin";
            var adminEmail = builder.Configuration["AdminAccount:Email"] ?? "admin@fittrack.local";

            if (!await roleManager.RoleExistsAsync(adminRole))
                await roleManager.CreateAsync(new IdentityRole(adminRole));

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Could not create the configured administrator: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            if (!await userManager.IsInRoleAsync(admin, adminRole))
                await userManager.AddToRoleAsync(admin, adminRole);
        }
    }
}


app.Run();
