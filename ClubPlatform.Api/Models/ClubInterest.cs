namespace ClubPlatform.Api.Models;

/// <summary>
/// Связь клуба и интереса.
/// Реализует связь M:N.
/// </summary>
public class ClubInterest
{
    public Guid ClubId { get; set; }

    public Guid InterestId { get; set; }

    public Club Club { get; set; } = null!;

    public Interest Interest { get; set; } = null!;
}