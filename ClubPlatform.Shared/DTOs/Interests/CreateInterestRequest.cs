using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Interests;

/// <summary>
/// Данные для создания нового интереса.
/// </summary>
public class CreateInterestRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Emoji { get; set; }
}