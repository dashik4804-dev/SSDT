namespace ClubPlatform.Api.Models;

/// <summary>
/// Участник клуба.
/// </summary>
public class ClubMember
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public bool IsMuted { get; set; }

    public Club Club { get; set; } = null!;

    public User User { get; set; } = null!;
}