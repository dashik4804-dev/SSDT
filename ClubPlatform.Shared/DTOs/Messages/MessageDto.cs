using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Shared.DTOs.Messages;

/// <summary>
/// Сообщение чата, которое отправляется клиенту.
/// </summary>
public class MessageDto
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Имя автора сообщения.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Роль автора сообщения.
    /// </summary>
    public UserRole AuthorRole { get; set; }

    /// <summary>
    /// URL фотографии автора.
    /// </summary>
    public string? UserPhotoUrl { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsDeleted { get; set; }
}