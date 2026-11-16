namespace ClubPlatform.Shared.Enums;

/// <summary>
/// Статус жалобы на сообщение.
/// </summary>
public enum ReportStatus
{
    /// <summary>
    /// Жалоба ещё не рассмотрена.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Жалоба обработана.
    /// </summary>
    Resolved = 1,

    /// <summary>
    /// Жалоба отклонена.
    /// </summary>
    Rejected = 2
}