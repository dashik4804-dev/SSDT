namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Ответ API на запрос проверки доступности сервера.
/// </summary>
public class ApiHealthResponse
{
    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset ServerTime { get; set; }
}