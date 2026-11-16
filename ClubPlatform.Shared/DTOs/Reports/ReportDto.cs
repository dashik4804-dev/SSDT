using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Reports;

/// <summary>
/// Данные жалобы для панели администратора.
/// </summary>
public class ReportDto
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid ClubId { get; set; }

    /// <summary>
    /// Название клуба.
    /// </summary>
    public string ClubName { get; set; } = string.Empty;

    /// <summary>
    /// ID автора сообщения.
    /// </summary>
    public Guid MessageAuthorUserId { get; set; }

    /// <summary>
    /// Имя автора сообщения.
    /// </summary>
    public string MessageAuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Роль автора сообщения.
    /// </summary>
    public UserRole MessageAuthorRole { get; set; }

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string MessageContent { get; set; } = string.Empty;

    public DateTimeOffset MessageCreatedAt { get; set; }

    /// <summary>
    /// Было ли сообщение уже удалено.
    /// </summary>
    public bool IsMessageDeleted { get; set; }

    /// <summary>
    /// Кто пожаловался.
    /// </summary>
    public Guid ReporterUserId { get; set; }

    public string ReporterName { get; set; } = string.Empty;

    /// <summary>
    /// Причина жалобы.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Можно ли выполнять действия с жалобой.
    /// Кнопки доступны только для новых жалоб со статусом Pending.
    /// </summary>
    public bool IsPending => Status == ReportStatus.Pending;
}