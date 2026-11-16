using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Api.Models;

/// <summary>
/// Пользователь приложения.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Здесь хранится BCrypt-хэш пароля.
    /// Сам пароль никогда не хранится.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string Name { get; set; } = string.Empty;

    public short Age { get; set; }

    public Gender Gender { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Навигационные свойства EF Core.

    public ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<Report> Reports { get; set; } = new List<Report>();
}