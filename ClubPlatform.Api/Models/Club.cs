namespace ClubPlatform.Api.Models;

/// <summary>
/// Клуб.
/// Все клубы приложения находятся в Москве.
/// Отдельного поля City здесь нет.
/// </summary>
public class Club
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Адрес клуба в Москве.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public int? MaxMembers { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ClubInterest> ClubInterests { get; set; }
        = new List<ClubInterest>();

    public ICollection<ClubMember> ClubMembers { get; set; }
        = new List<ClubMember>();

    public ICollection<Message> Messages { get; set; }
        = new List<Message>();
}