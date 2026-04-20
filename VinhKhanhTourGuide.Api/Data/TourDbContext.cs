using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Data
{
    public class TourDbContext : DbContext
    {
        public TourDbContext(DbContextOptions<TourDbContext> options) : base(options) { }

        // Đại diện cho cái bảng Poi trong SQL Server
        public DbSet<Poi> Poi { get; set; }

        public DbSet<ListeningLog> ListeningLogs { get; set; }
        public DbSet<VisitorActivity> VisitorActivities { get; set; }
    }
}
