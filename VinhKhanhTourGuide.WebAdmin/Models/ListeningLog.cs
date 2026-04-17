using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    [Table("ListeningLog")]
    public class ListeningLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PoiId { get; set; }

        [Required]
        public string AnonymousSessionId { get; set; }

        public double DurationSeconds { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime ListenAt { get; set; } = DateTime.Now;
    }
}