namespace ClubPlatform.Api.Models;

/// <summary>
/// Сообщение в чате клуба.
/// </summary>
public class Message
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Soft delete.
    /// true означает, что сообщение удалено модератором.
    /// </summary>
    public bool IsDeleted { get; set; }

    public Club Club { get; set; } = null!;

    public User User { get; set; } = null!;

    public ICollection<Report> Reports { get; set; }
        = new List<Report>();
}