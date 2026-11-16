
using ClubPlatform.Shared.DTOs.Support;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Модель для отправки ответа администратора на обращение.
/// </summary>
public class RespondSupportTicketViewModel
{
    public string Response { get; set; } = string.Empty;
}
