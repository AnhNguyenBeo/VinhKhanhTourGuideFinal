using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    [Table("RoutePoint")]
    public class RoutePoint
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AnonymousSessionId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double? AccuracyMeters { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}