
using ClubPlatform.Shared.Enums;
using Microsoft.Maui.Graphics;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Модель сообщения для отображения в мобильном чате.
/// </summary>
public sealed class ChatMessageItem
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid ClubId { get; set; }

    // ID автора сообщения, полученный от сервера.
    public Guid UserId { get; set; }

    // Роль автора сообщения.
    public UserRole AuthorRole { get; set; }

    public string? UserPhotoUrl { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime SentAt { get; set; }

    public bool IsDeleted { get; set; }

    // Устанавливается мобильным приложением.
    public bool IsOwnMessage { get; set; }

    public bool IsAdminMessage =>
        AuthorRole == UserRole.Admin;

    public Guid EffectiveId =>
        Id != Guid.Empty ? Id : MessageId;

    public string DisplayAuthorName =>
        !string.IsNullOrWhiteSpace(AuthorName) ? AuthorName
        : !string.IsNullOrWhiteSpace(SenderName) ? SenderName
        : !string.IsNullOrWhiteSpace(UserName) ? UserName
        : "Пользователь";

    public string DisplayContent =>
        IsDeleted
            ? "Сообщение удалено модератором."
            : !string.IsNullOrWhiteSpace(Content)
                ? Content
                : Text;

    public string TimeText
    {
        get
        {
            var date = CreatedAt != default
                ? CreatedAt
                : SentAt;

            return date == default
                ? string.Empty
                : date.ToLocalTime().ToString("HH:mm");
        }
    }

    // Цвет сообщения в зависимости от автора.
    public Color BubbleColor =>
        IsDeleted
            ? Color.FromArgb("#EEEAF4")
            : IsOwnMessage
                ? Color.FromArgb("#6235F5")
                : IsAdminMessage
                    ? Color.FromArgb("#DDF7FF")
                    : Colors.White;

    public Color MessageTextColor =>
        IsDeleted
            ? Color.FromArgb("#746D8A")
            : IsOwnMessage
                ? Colors.White
                : IsAdminMessage
                    ? Color.FromArgb("#185B77")
                    : Color.FromArgb("#292344");

    public Color AuthorTextColor =>
        IsOwnMessage && !IsDeleted
            ? Colors.White
            : IsAdminMessage && !IsDeleted
                ? Color.FromArgb("#087CA3")
                : Color.FromArgb("#6235F5");

    public Color TimeColor =>
        IsOwnMessage && !IsDeleted
            ? Color.FromArgb("#EDE4FF")
            : Color.FromArgb("#746D8A");

    public LayoutOptions BubbleAlignment =>
        IsOwnMessage
            ? LayoutOptions.End
            : LayoutOptions.Start;

    public string AuthorDisplayText =>
        IsAdminMessage
            ? $"🛡️ Администратор · {DisplayAuthorName}"
            : IsOwnMessage
                ? "Вы"
                : DisplayAuthorName;
}
