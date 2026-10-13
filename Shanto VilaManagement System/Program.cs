using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Shanto_VilaManagement_System.Data;
using Shanto_VilaManagement_System.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud Port Binding (Render, Railway, etc.)
var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort))
{
    builder.WebHost.UseUrls($"http://*:{envPort}");
}

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Database Connection (MySQL / XAMPP)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "server=127.0.0.1;port=3306;database=shanto_vila_db;user=root;password=;CharSet=utf8mb4;";

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = ServerVersion.Parse("10.4.32-mariadb");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// Add custom application services
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

// Add MVC services with Unicode UTF-8 character support for Bengali
builder.Services.AddSingleton(System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-migrate & seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "ডাটাবেজ ইনিশিয়ালাইজ করার সময় ত্রুটি হয়েছে।");
    }
}

// Configure HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.Equals("sw.js", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
