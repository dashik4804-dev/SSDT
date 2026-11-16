using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Messages;

/// <summary>
/// Данные для отправки сообщения.
/// </summary>
public class SendMessageRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}