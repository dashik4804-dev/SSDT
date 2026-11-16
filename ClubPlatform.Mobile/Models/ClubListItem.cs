using System.Linq;

namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Данные клуба, необходимые для карточки в каталоге.
/// </summary>
public sealed class ClubListItem
{
    private int _memberCount;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string? PhotoUrl { get; set; }

    // Показываем фотографию только при наличии корректной ссылки.
    public bool HasPhoto =>
        Uri.TryCreate(
            PhotoUrl,
            UriKind.Absolute,
            out var photoUri)
        && (photoUri.Scheme == Uri.UriSchemeHttp
            || photoUri.Scheme == Uri.UriSchemeHttps);

    public int? MaxMembers { get; set; }

    // Показывает, состоит ли текущий пользователь в клубе.
    public bool IsMember { get; set; }

    // Контактные данные клуба — если сервер их возвращает.
    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    // Поддерживаем оба распространённых имени свойства,
    // чтобы отображение количества участников не зависело
    // от названия поля в ответе API.
    public int MemberCount
    {
        get => _memberCount;
        set => _memberCount = value;
    }

    public int MembersCount
    {
        get => _memberCount;
        set => _memberCount = value;
    }

    public List<InterestListItem> Interests { get; set; } = new();

    public string MembersText =>
    MaxMembers.HasValue && MaxMembers.Value > 0
        ? $"Участников: {_memberCount} из {MaxMembers.Value}"
        : $"Участников: {_memberCount} (без ограничения)";

    public string InterestsText =>
        Interests.Count == 0
            ? "Интересы не указаны"
            : string.Join(
                " · ",
                Interests.Select(x => x.DisplayName));

    public bool HasAddress =>
        !string.IsNullOrWhiteSpace(Address);
}