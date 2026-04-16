using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching; // Cần thiết cho DispatcherTimer
using VinhKhanhTourGuide.Models;

namespace VinhKhanhTourGuide.Services
{
    public class GeofenceService
    {
        private IDispatcherTimer _radarTimer;
        private List<Poi> _poiList = new();
        private Dictionary<string, DateTime> _spokenPoisDict = new();
        private readonly int _cooldownMinutes = 2;
        private bool _isProcessing = false;

        // Sự kiện (Event) bắn ra khi phát hiện người dùng vào vùng Geofence hợp lệ
        public event EventHandler<(Poi targetPoi, Location userLocation)> PoiDetected;

        public void StartRadar(List<Poi> pois)
        {
            _poiList = pois;

            if (_radarTimer != null) return;

            // Sử dụng Dispatcher của Application để chạy ngầm
            _radarTimer = Application.Current.Dispatcher.CreateTimer();
            _radarTimer.Interval = TimeSpan.FromSeconds(5);
            _radarTimer.Tick += async (s, e) => await CheckGeofenceAsync();
            _radarTimer.Start();
        }

        public void StopRadar()
        {
            if (_radarTimer != null)
            {
                _radarTimer.Stop();
                _radarTimer = null;
            }
        }

        // Khóa radar tạm thời trong lúc App đang đọc thuyết minh
        public void SetProcessingState(bool isProcessing)
        {
            _isProcessing = isProcessing;
        }

        private async Task CheckGeofenceAsync()
        {
            if (_isProcessing) return; // Nếu đang bận đọc TTS thì không quét nữa

            try
            {
                var userLocation = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(3)));
                if (userLocation == null) return;

                var poisInRange = new List<(Poi poi, double distance)>();
                foreach (var poi in _poiList)
                {
                    double dist = Location.CalculateDistance(userLocation, new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;
                    if (dist <= poi.GeofenceRadius)
                    {
                        poisInRange.Add((poi, dist));
                    }
                }

                if (poisInRange.Count > 0)
                {
                    // Ưu tiên quán có Priority nhỏ, sau đó mới xét khoảng cách
                    var targetPoi = poisInRange.OrderBy(p => p.poi.Priority).ThenBy(p => p.distance).First().poi;

                    // Kiểm tra Cooldown 5 phút
                    if (_spokenPoisDict.TryGetValue(targetPoi.Id, out DateTime lastTime))
                    {
                        if ((DateTime.Now - lastTime).TotalMinutes < _cooldownMinutes) return;
                    }

                    _spokenPoisDict[targetPoi.Id] = DateTime.Now;

                    // BẮN SỰ KIỆN GỌI MAP-PAGE XỬ LÝ
                    PoiDetected?.Invoke(this, (targetPoi, userLocation));
                }
            }
            catch (Exception) { /* Bỏ qua lỗi mất sóng GPS tạm thời */ }
        }
    }
}