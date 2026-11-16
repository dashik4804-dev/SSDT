
namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Информация о пользователе, заблокированном в клубе.
/// </summary>
public class BannedMemberItem
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? UserPhotoUrl { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    // Дата блокировки в удобном формате.
    public string CreatedAtText =>
        CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
