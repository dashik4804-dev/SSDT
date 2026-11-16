namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Интерес для отображения в фильтре каталога.
/// </summary>
public sealed class InterestListItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Emoji { get; set; } = string.Empty;

    // Название интереса вместе с эмодзи для списка выбора.
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Emoji)
            ? Name
            : $"{Emoji} {Name}";
}