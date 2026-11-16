namespace ClubPlatform.Mobile.Services.Storage;

/// <summary>
/// Интерфейс безопасного хранения JWT-токена.
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Сохраняет токен пользователя.
    /// </summary>
    Task SaveTokenAsync(string token);

    /// <summary>
    /// Получает сохранённый токен.
    /// Если токена нет, возвращает null.
    /// </summary>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Удаляет сохранённый токен при выходе.
    /// </summary>
    Task RemoveTokenAsync();
}