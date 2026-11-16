using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.ClubSuggestions;

/// <summary>
/// Запрос пользователя на создание нового клуба.
/// </summary>
public sealed class CreateClubSuggestionRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? InterestName { get; set; }

    /// <summary>
    /// Предполагаемый адрес в Москве. Заполнять необязательно.
    /// </summary>
    [StringLength(500)]
    public string? SuggestedAddress { get; set; }
}