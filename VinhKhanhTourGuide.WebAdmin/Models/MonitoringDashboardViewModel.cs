using System;
using System.Collections.Generic;

namespace VinhKhanhTourGuide.WebAdmin.Models
{
    public class MonitoringDashboardViewModel
    {
        public int ActiveUsersNow { get; set; }
        public int NearPoiUsersNow { get; set; }
        public int ListeningUsersNow { get; set; }
        public int TotalPois { get; set; }
        public int TotalListens { get; set; }
        public int ListensToday { get; set; }
        public int UniqueVisitorsToday { get; set; }
        public double AverageDurationToday { get; set; }
        public int ShortListensToday { get; set; }
        public DateTime? LastListenAt { get; set; }
        public string HourlyLabelsJson { get; set; } = "[]";
        public string HourlyCountsJson { get; set; } = "[]";
        public List<MonitoringTopPoiViewModel> TopPoisToday { get; set; } = new();
        public List<MonitoringRecentLogViewModel> RecentLogs { get; set; } = new();
        public List<MonitoringActiveVisitorViewModel> ActiveVisitors { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
    }

    public class MonitoringTopPoiViewModel
    {
        public string PoiName { get; set; } = string.Empty;
        public int TotalListens { get; set; }
        public double AverageDuration { get; set; }
    }

    public class MonitoringRecentLogViewModel
    {
        public string PoiName { get; set; } = string.Empty;
        public string AnonymousSessionId { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public DateTime ListenAt { get; set; }
    }

    public class MonitoringActiveVisitorViewModel
    {
        public string AnonymousSessionId { get; set; } = string.Empty;
        public string Status { get; set; } = "app_open";
        public string? NearestPoiName { get; set; }
        public double? DistanceToNearestPoiMeters { get; set; }
        public string? CurrentListeningPoiName { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
