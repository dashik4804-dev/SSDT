
using System;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Модель карточки обращения для панели администратора.
/// </summary>
public class AdminSupportTicketItem
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? AdminResponse { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string ResponseDraft { get; set; } = string.Empty;

    public bool HasResponse =>
        !string.IsNullOrWhiteSpace(AdminResponse);

    public string StatusText => Status switch
    {
        "Pending" => "Ожидает ответа",
        "Answered" => "Ответ отправлен",
        "Closed" => "Закрыто",
        _ => Status
    };

    public string CreatedAtText =>
        $"Дата обращения: {CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public bool CanAnswer => Status == "Pending";
}
