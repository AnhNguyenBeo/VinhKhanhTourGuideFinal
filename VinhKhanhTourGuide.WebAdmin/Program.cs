using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<VinhKhanhTourGuide.WebAdmin.Data.TourDbContext>(
    options => options.UseSqlServer(connectionString)
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<VinhKhanhTourGuide.WebAdmin.Data.TourDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Không thể tự động apply migration khi khởi động WebAdmin.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ FIX: Chỉ redirect HTTPS trên Production
// Khi Development, giữ HTTP để app Android (10.0.2.2) gọi được bình thường
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(); // Phục vụ ảnh từ wwwroot/images/

app.UseRouting();

app.UseAuthorization();

app.MapGet("/health", async (VinhKhanhTourGuide.WebAdmin.Data.TourDbContext db, IWebHostEnvironment env) =>
{
    try
    {
        bool databaseOk = await db.Database.CanConnectAsync();
        string imagesPath = Path.Combine(env.WebRootPath, "images");

        var payload = new
        {
            service = "webadmin",
            status = databaseOk ? "ok" : "degraded",
            checkedAt = DateTimeOffset.UtcNow,
            database = databaseOk ? "ok" : "unreachable",
            imageStorage = Directory.Exists(imagesPath) ? "ok" : "missing",
            processStartedAt = System.Diagnostics.Process.GetCurrentProcess().StartTime
        };

        return databaseOk
            ? Results.Ok(payload)
            : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            service = "webadmin",
            status = "down",
            checkedAt = DateTimeOffset.UtcNow,
            database = "error",
            message = ex.Message
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Cố định culture en-US để dấu chấm thập phân (Latitude/Longitude) không bị lỗi
var supportedCultures = new[] { "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.Run();
