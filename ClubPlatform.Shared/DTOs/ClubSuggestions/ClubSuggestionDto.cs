namespace ClubPlatform.Shared.DTOs.ClubSuggestions;

/// <summary>
/// Информация о предложении клуба.
/// </summary>
public sealed class ClubSuggestionDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? CreatedClubId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? InterestName { get; set; }

    public string? SuggestedAddress { get; set; }

    public string Status { get; set; } = "Pending";

    public string? AdminComment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }
}