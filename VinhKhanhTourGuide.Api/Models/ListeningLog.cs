using System;
using System.ComponentModel.DataAnnotations.Schema; // Nhớ thêm dòng này

namespace VinhKhanhTourGuide.Api.Models
{
    [Table("ListeningLog")] // Ép API hiểu đúng tên bảng trong SQL Server
    public class ListeningLog
    {
        public int Id { get; set; }
        public string PoiId { get; set; }
        public string AnonymousSessionId { get; set; }
        public double DurationSeconds { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ListenAt { get; set; } = DateTime.Now;
    }
}