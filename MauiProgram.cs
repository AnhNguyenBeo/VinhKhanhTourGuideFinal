using Microsoft.Extensions.Logging;

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
            // Đăng ký Services
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.TtsService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Services.TranslationService>();
            builder.Services.AddSingleton<VinhKhanhTourGuide.Data.AppDbContext>();
            // Đăng ký Views
            builder.Services.AddTransient<VinhKhanhTourGuide.Views.MapPage>();
            return builder.Build();
        }
    }
}
