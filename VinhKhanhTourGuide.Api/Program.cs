using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using VinhKhanhTourGuide.Api.Data;
using VinhKhanhTourGuide.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5099");

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TourDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<SharedTranslationService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<TourDbContext>();
        await EnsureVisitorActivityTableAsync(db);
        await EnsureTranslationCacheTableAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Khong the dam bao cac bang monitor/translation khi khoi dong API.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

string webAdminImagesPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "VinhKhanhTourGuide.WebAdmin",
    "wwwroot",
    "images");

if (Directory.Exists(webAdminImagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webAdminImagesPath),
        RequestPath = "/images"
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

static async Task EnsureTranslationCacheTableAsync(TourDbContext db)
{
    const string sql = """
IF OBJECT_ID(N'[dbo].[TranslationCache]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TranslationCache]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PoiId] NVARCHAR(256) NOT NULL,
        [LanguageCode] NVARCHAR(16) NOT NULL,
        [TranslatedText] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_TranslationCache] PRIMARY KEY ([Id])
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TranslationCache_PoiId_LanguageCode'
      AND object_id = OBJECT_ID(N'[dbo].[TranslationCache]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_TranslationCache_PoiId_LanguageCode]
    ON [dbo].[TranslationCache] ([PoiId], [LanguageCode]);
END
""";

    await db.Database.ExecuteSqlRawAsync(sql);
}
