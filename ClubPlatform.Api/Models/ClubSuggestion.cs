namespace ClubPlatform.Api.Models;

/// <summary>
/// Предложение пользователя о создании нового клуба.
/// </summary>
public class ClubSuggestion
{
    public Guid Id { get; set; }

    // Пользователь, отправивший предложение.
    public Guid UserId { get; set; }

    // Предлагаемое название клуба.
    public string Name { get; set; } = string.Empty;

    // Описание идеи и её цели.
    public string Description { get; set; } = string.Empty;

    // Интерес, к которому относится предложение.
    public string? InterestName { get; set; }

    /// <summary>
    /// Предлагаемый адрес клуба в Москве, если он известен.
    /// </summary>
    public string? SuggestedAddress { get; set; }

    // Pending, Approved или Rejected.
    public string Status { get; set; } = "Pending";

    // Ответ администратора, если он есть.
    public string? AdminComment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    // ID клуба, созданного по предложению, если он уже создан.
    public Guid? CreatedClubId { get; set; }

    // Связь с созданным клубом.
    public Club? CreatedClub { get; set; }

    // Навигационное свойство пользователя.
    public User User { get; set; } = null!;
}