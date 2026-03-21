namespace VinhKhanhTourGuide.Models
{
    public class Poi
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description_VN { get; set; } // Dòng này cực kỳ quan trọng
        public int GeofenceRadius { get; set; }
    }
}