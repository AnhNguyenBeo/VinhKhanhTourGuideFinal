using System;
using System.Collections.Generic;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    public class AnalyticsDashboardViewModel
    {
        public int ActiveWindowSeconds { get; set; }
        public DateTime SnapshotGeneratedAt { get; set; }
        public int ActiveUsersNow { get; set; }
        public List<AnalyticsActiveVisitorViewModel> ActiveVisitors { get; set; } = new();
        public List<AnalyticsViewModel> PoiStats { get; set; } = new();
        public string HeatmapDataJson { get; set; } = "[]";
    }

    public class AnalyticsViewModel
    {
        public string PoiName { get; set; } = string.Empty;
        public int TotalListens { get; set; }
        public double AverageDuration { get; set; }
    }

    public class AnalyticsActiveVisitorViewModel
    {
        public string AnonymousSessionId { get; set; } = string.Empty;
        public string Status { get; set; } = "app_open";
        public string? NearestPoiName { get; set; }
        public double? DistanceToNearestPoiMeters { get; set; }
        public string? CurrentListeningPoiName { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
