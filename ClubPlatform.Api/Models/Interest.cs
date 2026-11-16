namespace ClubPlatform.Api.Models;

/// <summary>
/// Интерес.
/// </summary>
public class Interest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Emoji { get; set; }

    public ICollection<ClubInterest> ClubInterests { get; set; }
        = new List<ClubInterest>();
}