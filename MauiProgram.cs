using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
#if ANDROID
using Android.Gms.Maps;
#endif

namespace VinhKhanhTourGuide
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.AppActivationService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.PremiumService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.PurchaseService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Data.AppDbContext>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.GeofenceService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.TtsService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.TranslationService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.VisitorActivityService>();
            builder.Services.AddTransient<VinhKhanhTourGuide.Views.MapPage>();

#if ANDROID
            MapHandler.Mapper.AppendToMapping("EnableZoomControls", (handler, view) =>
            {
                handler.PlatformView.GetMapAsync(new MapReadyCallback());
            });
#endif

            return builder.Build();
        }
    }

#if ANDROID
    internal class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        public void OnMapReady(GoogleMap googleMap)
        {
            googleMap.UiSettings.ZoomControlsEnabled = true;
            googleMap.UiSettings.ZoomGesturesEnabled = true;
            googleMap.UiSettings.CompassEnabled = true;
        }
    }
#endif
}
