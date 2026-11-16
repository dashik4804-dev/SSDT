using ClubPlatform.Shared.DTOs.Interests;

namespace ClubPlatform.Shared.DTOs.Clubs;

/// <summary>
/// Данные клуба, которые возвращаются клиенту.
/// </summary>
public class ClubDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Адрес клуба в Москве.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public int? MaxMembers { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Текущее количество участников.
    /// </summary>
    public int MembersCount { get; set; }

    /// <summary>
    /// Является ли текущий пользователь участником клуба.
    /// Для администратора всегда false.
    /// </summary>
    public bool IsMember { get; set; }

    /// <summary>
    /// Интересы клуба.
    /// </summary>
    public List<InterestDto> Interests { get; set; } = [];
}