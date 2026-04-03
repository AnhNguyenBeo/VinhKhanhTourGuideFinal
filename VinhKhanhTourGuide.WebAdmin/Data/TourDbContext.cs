using Microsoft.EntityFrameworkCore;
using VinhKhanhTourGuide.WebAdmin.Models;

namespace VinhKhanhTourGuide.WebAdmin.Data
{
    public class TourDbContext : DbContext
    {
        public TourDbContext(DbContextOptions<TourDbContext> options) : base(options) { }
        public DbSet<Poi> Poi { get; set; }

        public DbSet<ListeningLog> ListeningLogs { get; set; }
    }
}