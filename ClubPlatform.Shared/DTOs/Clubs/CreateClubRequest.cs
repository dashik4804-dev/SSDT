using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Clubs;

/// <summary>
/// Данные для создания клуба.
/// </summary>
public class CreateClubRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Url]
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Адрес клуба в Москве.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [MaxLength(320)]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Максимальное количество участников.
    /// null означает отсутствие ограничения.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? MaxMembers { get; set; }

    /// <summary>
    /// ID интересов, связанных с клубом.
    /// </summary>
    public List<Guid> InterestIds { get; set; } = [];
}