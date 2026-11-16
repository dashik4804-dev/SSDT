
using System;

namespace ClubPlatform.Api.Models;

/// <summary>
/// Обращение пользователя в службу поддержки.
/// </summary>
public class SupportTicket
{
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего обращение.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Тема обращения.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Текст обращения пользователя.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Статус: Pending, Answered или Closed.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Ответ администратора.
    /// </summary>
    public string? AdminResponse { get; set; }

    /// <summary>
    /// Дата создания обращения.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Дата ответа администратора.
    /// </summary>
    public DateTimeOffset? AnsweredAt { get; set; }

    /// <summary>
    /// Пользователь, создавший обращение.
    /// </summary>
    public User User { get; set; } = null!;
}
