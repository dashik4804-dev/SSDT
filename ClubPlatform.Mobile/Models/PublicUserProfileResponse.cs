
namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Ответ API с публичным профилем другого пользователя.
/// </summary>
public class PublicUserProfileResponse
{
    public PublicUserInfo User { get; set; } = new();

    public List<PublicUserClubItem> Clubs { get; set; } = new();
}

/// <summary>
/// Публичная информация о пользователе.
/// </summary>
public class PublicUserInfo
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public short Age { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }
}

/// <summary>
/// Клуб, в котором состоит пользователь.
/// </summary>
public class PublicUserClubItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    public string Address { get; set; } = string.Empty;
}
