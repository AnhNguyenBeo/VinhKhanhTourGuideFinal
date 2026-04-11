using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.Api.Models;

namespace VinhKhanhTourGuide.Api.Data
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TourPoi>()
                .HasIndex(x => new { x.TourId, x.PoiId })
                .IsUnique();

            modelBuilder.Entity<TranslationEntry>()
                .HasIndex(x => new { x.PoiId, x.LanguageCode })
                .IsUnique();

            modelBuilder.Entity<AudioAsset>()
                .HasIndex(x => new { x.PoiId, x.LanguageCode });

            modelBuilder.Entity<RoutePoint>()
                .HasIndex(x => new { x.AnonymousSessionId, x.RecordedAt });
        }
    }
}