namespace ClubPlatform.Shared.DTOs.Reports;

/// <summary>
/// Пользователь, которому запрещена отправка сообщений
/// в конкретном клубе.
/// </summary>
public sealed class MutedClubMemberDto
{
    public Guid ClubId { get; set; }

    public string ClubName { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTimeOffset JoinedAt { get; set; }
}