namespace ClubPlatform.Shared.DTOs.Interests;

/// <summary>
/// Данные интереса, которые API отдаёт клиенту.
/// </summary>
public class InterestDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Emoji { get; set; }
}