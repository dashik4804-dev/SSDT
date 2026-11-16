using ClubPlatform.Shared.DTOs.ClubSuggestions;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Данные предложения в интерфейсе мобильного приложения.
/// </summary>
public sealed class ClubSuggestionListItem
{
    public ClubSuggestionDto Suggestion { get; set; } = new();

    public Guid Id => Suggestion.Id;

    public string UserName => Suggestion.UserName;

    public string Name => Suggestion.Name;

    public string Description => Suggestion.Description;

    public string? InterestName => Suggestion.InterestName;

    public bool HasSuggestedAddress =>
    !string.IsNullOrWhiteSpace(Suggestion.SuggestedAddress);

    public string SuggestedAddressText =>
        $"Предлагаемый адрес: {Suggestion.SuggestedAddress}";

    public Guid? CreatedClubId => Suggestion.CreatedClubId;

    public string StatusText =>
        CreatedClubId.HasValue
            ? "Клуб создан"
            : Suggestion.Status switch
            {
                "Pending" => "На рассмотрении",
                "Approved" => "Одобрено",
                "Rejected" => "Отклонено",
                _ => Suggestion.Status
            };

    public bool CanCreateClub =>
        string.Equals(
            Suggestion.Status,
            "Approved",
            StringComparison.OrdinalIgnoreCase)
        && !CreatedClubId.HasValue;

    public string CreatedAtText =>
        $"Отправлено: {Suggestion.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

    public bool HasInterest =>
        !string.IsNullOrWhiteSpace(Suggestion.InterestName);

    public string InterestText =>
        $"Интерес: {Suggestion.InterestName}";

    public bool HasAdminComment =>
        !string.IsNullOrWhiteSpace(Suggestion.AdminComment);

    public string AdminCommentText =>
        $"Ответ администратора: {Suggestion.AdminComment}";

    public bool CanReview =>
        string.Equals(
            Suggestion.Status,
            "Pending",
            StringComparison.OrdinalIgnoreCase);
}
