
using Microsoft.Extensions.Logging;
using ClubPlatform.Mobile.Constants;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Mobile.Views;

namespace ClubPlatform.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Настройка MAUI
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // =========================================
            // HTTP CLIENT
            // =========================================

            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(ApiConstants.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            });

            // =========================================
            // СЕРВИСЫ
            // =========================================

            builder.Services.AddSingleton<IApiService, ApiService>();

            builder.Services.AddSingleton<
                ITokenStorage,
                SecureTokenStorage>();

            // =========================================
            // СТРАНИЦЫ
            // =========================================

            builder.Services.AddTransient<WelcomePage>();

            // =========================================
            // ПРИЛОЖЕНИЕ
            // =========================================

            builder.Services.AddSingleton<App>();

            return builder.Build();
        }
    }
}
