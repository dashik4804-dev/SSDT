using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Users;

/// <summary>
/// Данные пользователя, которые разрешено
/// отправлять мобильному приложению.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public short Age { get; set; }

    public Gender Gender { get; set; }

    public string? PhotoUrl { get; set; }

    public UserRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}