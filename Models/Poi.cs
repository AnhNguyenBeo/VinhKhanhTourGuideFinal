// Models/Poi.cs
using SQLite;

namespace VinhKhanhTourGuide.Models
{
    public class Poi
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string? Name { get; set; }
        public string ImageName { get; set; }
        public string ImageUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description_VN { get; set; }
        public int GeofenceRadius { get; set; }
        public double Distance { get; set; }
        public string DistanceText =>
    Distance < 1000
        ? $"{Distance:F0} m"
        : $"{Distance / 1000:F1} km";

        // TÍNH NĂNG MỚI: Mức ưu tiên (Priority)
        public int Priority { get; set; }
    }
}