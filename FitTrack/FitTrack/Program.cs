using FitTrack.Data;
using Microsoft.EntityFrameworkCore;
using FitTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using FitTrack.Services;
using System.IO;
using System.Linq;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var entry = line.Trim();
        if (entry.Length == 0 || entry.StartsWith('#') || !entry.Contains('=')) continue;
        var separator = entry.IndexOf('=');
        var key = entry[..separator].Trim();
        var value = entry[(separator + 1)..].Trim().Trim('"', '\'');
        if (key.Length > 0 && Environment.GetEnvironmentVariable(key) is null) Environment.SetEnvironmentVariable(key, value);
    }
}
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

builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/Account/Login");
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AuthorizeFilter()));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("OpenAI:TimeoutSeconds", 90));
});
builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/openai/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Gemini:TimeoutSeconds", 90));
});
builder.Services.AddHttpClient("Wger", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExerciseApi:BaseUrl"] ?? "https://wger.de/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("ExerciseApi:TimeoutSeconds", 20));
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FitTrack/1.0");
});
builder.Services.AddScoped<IExerciseApiService, ExerciseApiService>();
builder.Services.AddScoped<IAITrainingService, AITrainingService>();
builder.Services.AddScoped<IWorkoutPlanService, WorkoutPlanService>();

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

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

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
