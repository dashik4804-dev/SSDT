
using System.ComponentModel.DataAnnotations;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Auth;

/// <summary>
/// Данные, которые пользователь может изменить в своём профиле.
/// Email, пароль и роль через этот запрос не изменяются.
/// </summary>
public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Укажи имя.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Имя должно содержать от 2 до 100 символов.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Возраст должен быть от 0 до 120.")]
    public short Age { get; set; }

    public Gender Gender { get; set; }

    [Url(ErrorMessage = "Укажи корректную ссылку на фотографию.")]
    [MaxLength(2048)]
    public string? PhotoUrl { get; set; }
}
