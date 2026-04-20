using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5099");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TourDbContext>(options => options.UseSqlServer(connectionString));
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<TourDbContext>();
        await EnsureVisitorActivityTableAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Khong the dam bao bang VisitorActivity khi khoi dong API.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
var webAdminImagesPath = Path.Combine(builder.Environment.ContentRootPath, "..", "VinhKhanhTourGuide.WebAdmin", "wwwroot", "images");

if (Directory.Exists(webAdminImagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webAdminImagesPath),
        RequestPath = "/images" // Bất cứ khi nào App gọi "/images/...", API sẽ tự chạy sang WebAdmin lấy
    });
}
app.UseAuthorization();

app.MapGet("/health", async (TourDbContext db) =>
{
    try
    {
        bool databaseOk = await db.Database.CanConnectAsync();

        var payload = new
        {
            service = "api",
            status = databaseOk ? "ok" : "degraded",
            checkedAt = DateTimeOffset.UtcNow,
            database = databaseOk ? "ok" : "unreachable",
            imageBridge = Directory.Exists(webAdminImagesPath) ? "ok" : "missing",
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
            service = "api",
            status = "down",
            checkedAt = DateTimeOffset.UtcNow,
            database = "error",
            message = ex.Message
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();

static async Task EnsureVisitorActivityTableAsync(TourDbContext db)
{
    const string sql = """
IF OBJECT_ID(N'[VisitorActivity]', N'U') IS NULL
BEGIN
    CREATE TABLE [VisitorActivity]
    (
        [AnonymousSessionId] NVARCHAR(450) NOT NULL,
        [Latitude] FLOAT NULL,
        [Longitude] FLOAT NULL,
        [NearestPoiId] NVARCHAR(MAX) NULL,
        [DistanceToNearestPoiMeters] FLOAT NULL,
        [Status] NVARCHAR(MAX) NOT NULL,
        [CurrentListeningPoiId] NVARCHAR(MAX) NULL,
        [LastEvent] NVARCHAR(MAX) NULL,
        [Platform] NVARCHAR(MAX) NULL,
        [LastSeenAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_VisitorActivity] PRIMARY KEY ([AnonymousSessionId])
    );
END
""";

    await db.Database.ExecuteSqlRawAsync(sql);
}
