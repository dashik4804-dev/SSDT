using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Api.Models;

/// <summary>
/// Жалоба пользователя на сообщение.
/// </summary>
public class Report
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid ReporterUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Message Message { get; set; } = null!;

    public User ReporterUser { get; set; } = null!;
}