using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.ClubSuggestions;

/// <summary>
/// Решение администратора по предложению клуба.
/// </summary>
public sealed class ReviewClubSuggestionRequest
{
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? AdminComment { get; set; }
}