
using System;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Модель карточки жалобы в личном кабинете пользователя.
/// </summary>
public class MyReportItem
{
    public Guid Id { get; set; }

    public string ClubName { get; set; } = string.Empty;

    public string MessageAuthorName { get; set; } = string.Empty;

    public string MessageContent { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string StatusText => Status switch
    {
        ReportStatus.Pending => "На рассмотрении",
        ReportStatus.Resolved => "Рассмотрена",
        ReportStatus.Rejected => "Отклонена",
        _ => "Неизвестный статус"
    };

    public string CreatedAtText =>
        $"Отправлена: {CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";
}
