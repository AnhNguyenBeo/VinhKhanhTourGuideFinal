using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Data
{
    public class TourDbContext : DbContext
    {
        public TourDbContext(DbContextOptions<TourDbContext> options) : base(options) { }

        public DbSet<Poi> Poi { get; set; }
        public DbSet<ListeningLog> ListeningLogs { get; set; }

        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourPoi> TourPois { get; set; }
        public DbSet<TranslationEntry> TranslationEntries { get; set; }
        public DbSet<AudioAsset> AudioAssets { get; set; }
        public DbSet<RoutePoint> RoutePoints { get; set; }
    }
}