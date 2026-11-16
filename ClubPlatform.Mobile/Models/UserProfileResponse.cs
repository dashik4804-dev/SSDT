
namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Ответ API с данными текущего пользователя.
/// </summary>
public sealed class UserProfileResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public short Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    public string Role { get; set; } = string.Empty;
}
