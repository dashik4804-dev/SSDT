
using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Support;

/// <summary>
/// Запрос администратора на ответ пользователю.
/// </summary>
public class RespondSupportTicketRequest
{
    [Required(ErrorMessage = "Напиши ответ пользователю.")]
    [StringLength(4000, MinimumLength = 1)]
    public string Response { get; set; } = string.Empty;
}
