
namespace ClubPlatform.Api.Models;

/// <summary>
/// Блокировка пользователя в конкретном клубе.
/// </summary>
public class ClubBan
{
    public Guid Id { get; set; }

    // Клуб, в котором пользователь заблокирован.
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    // Пользователь, которому запрещено вступать.
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Администратор, который выдал блокировку.
    public Guid BannedByUserId { get; set; }

    // Причина блокировки.
    public string Reason { get; set; } = string.Empty;

    // Дата блокировки.
    public DateTimeOffset CreatedAt { get; set; }
}
