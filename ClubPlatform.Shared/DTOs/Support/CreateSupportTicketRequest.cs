
using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Support;

/// <summary>
/// Запрос пользователя на создание обращения в поддержку.
/// </summary>
public class CreateSupportTicketRequest
{
    [Required(ErrorMessage = "Укажи тему обращения.")]
    [StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Напиши текст обращения.")]
    [StringLength(4000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}
