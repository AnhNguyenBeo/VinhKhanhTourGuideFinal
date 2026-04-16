using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Data;
using Microsoft.EntityFrameworkCore;

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

app.MapControllers();

app.Run();
