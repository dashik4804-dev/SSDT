
namespace ClubPlatform.Mobile.Constants;

/// <summary>
/// Настройки подключения мобильного приложения
/// к Backend через USB (ADB reverse).
/// </summary>
public static class ApiConstants
{
    // При активном ADB reverse запросы
    // перенаправляются на компьютер.
    public const string BaseUrl = "http://127.0.0.1:5253/";
}
