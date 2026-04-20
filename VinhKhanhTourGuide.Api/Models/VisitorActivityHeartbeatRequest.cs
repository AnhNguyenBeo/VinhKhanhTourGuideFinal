namespace VinhKhanhTourGuide.Api.Models
{
    public class VisitorActivityHeartbeatRequest
    {
        public string AnonymousSessionId { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? NearestPoiId { get; set; }
        public double? DistanceToNearestPoiMeters { get; set; }
        public string Status { get; set; } = "app_open";
        public string? CurrentListeningPoiId { get; set; }
        public string? LastEvent { get; set; }
        public string? Platform { get; set; }
    }
}
