using System.ComponentModel.DataAnnotations;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Auth;

/// <summary>
/// Данные для регистрации нового пользователя.
/// </summary>
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120)]
    public short Age { get; set; }

    public Gender Gender { get; set; }

    /// <summary>
    /// HTTPS-ссылка на фотографию,
    /// предварительно загруженную в Cloudinary.
    /// </summary>
    [Required]
    [Url]
    public string PhotoUrl { get; set; } = string.Empty;
}