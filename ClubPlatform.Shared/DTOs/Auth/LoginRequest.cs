using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Auth;

/// <summary>
/// Данные для входа.
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}