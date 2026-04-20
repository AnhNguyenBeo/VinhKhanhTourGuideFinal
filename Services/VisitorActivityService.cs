using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhTourGuide.Data;
using VinhKhanhTourGuide.Models;

namespace VinhKhanhTourGuide.Services
{
    public class VisitorActivityService
    {
        private readonly AppDbContext _dbContext;
        private readonly SemaphoreSlim _heartbeatLock = new SemaphoreSlim(1, 1);
        private IDispatcherTimer? _heartbeatTimer;
        private List<Poi> _poiList = new();
        private bool _isListening;
        private string? _currentListeningPoiId;

        private const int HeartbeatIntervalSeconds = 15;
        private const double NearPoiThresholdMeters = 100;

        public VisitorActivityService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void StartTracking(IEnumerable<Poi> pois)
        {
            _poiList = pois.ToList();

            if (_heartbeatTimer != null)
            {
                return;
            }

            _heartbeatTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_heartbeatTimer == null)
            {
                return;
            }

            _heartbeatTimer.Interval = TimeSpan.FromSeconds(HeartbeatIntervalSeconds);
            _heartbeatTimer.Tick += async (_, _) => await SendHeartbeatSafeAsync("heartbeat");
            _heartbeatTimer.Start();

            _ = SendHeartbeatSafeAsync("app_open");
        }

        public void UpdatePoiList(IEnumerable<Poi> pois)
        {
            _poiList = pois.ToList();
        }

        public void SetListeningState(bool isListening, string? poiId = null)
        {
            _isListening = isListening;
            _currentListeningPoiId = isListening ? poiId : null;
            _ = SendHeartbeatSafeAsync(isListening ? "narration_started" : "narration_stopped");
        }

        public async Task SendImmediateHeartbeatAsync(string lastEvent = "heartbeat")
        {
            await SendHeartbeatSafeAsync(lastEvent);
        }

        private async Task SendHeartbeatSafeAsync(string lastEvent)
        {
            if (!await _heartbeatLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                await SendHeartbeatCoreAsync(lastEvent);
            }
            finally
            {
                _heartbeatLock.Release();
            }
        }

        private async Task SendHeartbeatCoreAsync(string lastEvent)
        {
            Location? location = null;

            try
            {
                location = await Geolocation.Default.GetLastKnownLocationAsync()
                    ?? await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));
            }
            catch
            {
                // Không chặn app nếu GPS tạm thời lỗi.
            }

            Poi? nearestPoi = null;
            double? nearestDistanceMeters = null;

            if (location != null && _poiList.Count > 0)
            {
                foreach (var poi in _poiList)
                {
                    var distanceMeters = Location.CalculateDistance(
                        location,
                        new Location(poi.Latitude, poi.Longitude),
                        DistanceUnits.Kilometers) * 1000;

                    if (!nearestDistanceMeters.HasValue || distanceMeters < nearestDistanceMeters.Value)
                    {
                        nearestDistanceMeters = distanceMeters;
                        nearestPoi = poi;
                    }
                }
            }

            var status = ResolveStatus(nearestDistanceMeters);

            var ping = new VisitorActivityPing
            {
                AnonymousSessionId = _dbContext.GetOrCreateSessionId(),
                Latitude = location?.Latitude,
                Longitude = location?.Longitude,
                NearestPoiId = nearestPoi?.Id,
                DistanceToNearestPoiMeters = nearestDistanceMeters.HasValue
                    ? Math.Round(nearestDistanceMeters.Value, 1)
                    : null,
                Status = status,
                CurrentListeningPoiId = _isListening ? _currentListeningPoiId : null,
                LastEvent = lastEvent,
                Platform = DeviceInfo.Platform.ToString()
            };

            await _dbContext.SendVisitorActivityAsync(ping);
        }

        private string ResolveStatus(double? nearestDistanceMeters)
        {
            if (_isListening && !string.IsNullOrWhiteSpace(_currentListeningPoiId))
            {
                return "listening";
            }

            if (nearestDistanceMeters.HasValue && nearestDistanceMeters.Value <= NearPoiThresholdMeters)
            {
                return "near_poi";
            }

            return "app_open";
        }
    }
}
