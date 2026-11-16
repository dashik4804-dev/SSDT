using System.ComponentModel.DataAnnotations;

namespace ClubPlatform.Shared.DTOs.Reports;

/// <summary>
/// Данные для создания жалобы на сообщение.
/// </summary>
public class CreateReportRequest
{
    /// <summary>
    /// ID сообщения, на которое пользователь жалуется.
    /// </summary>
    [Required]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Причина жалобы.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}