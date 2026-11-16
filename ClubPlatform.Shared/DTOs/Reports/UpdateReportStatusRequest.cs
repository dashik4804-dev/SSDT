using System.ComponentModel.DataAnnotations;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Reports;

/// <summary>
/// Изменение статуса жалобы администратором.
/// </summary>
public class UpdateReportStatusRequest
{
    [Required]
    public ReportStatus Status { get; set; }
}