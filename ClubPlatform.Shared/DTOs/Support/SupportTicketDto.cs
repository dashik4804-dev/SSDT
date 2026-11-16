
namespace ClubPlatform.Shared.DTOs.Support;

/// <summary>
/// Информация об обращении в службу поддержки.
/// </summary>
public class SupportTicketDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? AdminResponse { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }
}
