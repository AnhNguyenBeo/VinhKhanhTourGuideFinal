using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Nhớ thêm dòng này để dùng thẻ [Table]

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    [Table("ListeningLog")] // "Bùa chú" ép EF Core không được tự ý thêm chữ 's'
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