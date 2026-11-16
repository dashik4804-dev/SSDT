
using ClubPlatform.Shared.DTOs.Support;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Модель обращения для отображения на мобильной странице.
/// </summary>
public class SupportTicketViewModel
{
    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? AdminResponse { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }

    public bool HasResponse =>
        !string.IsNullOrWhiteSpace(AdminResponse);

    public string StatusText => Status switch
    {
        "Pending" => "Ожидает рассмотрения",
        "Answered" => "Получен ответ",
        "Closed" => "Закрыто",
        _ => Status
    };

    public string CreatedAtText =>
        $"Отправлено: {CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string AnsweredAtText =>
        AnsweredAt.HasValue
            ? $"Ответ: {AnsweredAt.Value.ToLocalTime():dd.MM.yyyy HH:mm}"
            : string.Empty;
}
