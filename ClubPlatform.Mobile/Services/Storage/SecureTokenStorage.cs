using Microsoft.Maui.Storage;

namespace ClubPlatform.Mobile.Services.Storage;

/// <summary>
/// Безопасное хранение JWT через SecureStorage MAUI.
/// </summary>
public class SecureTokenStorage : ITokenStorage
{
    // Название ключа, под которым хранится JWT.
    private const string TokenKey = "club_platform_jwt";

    /// <summary>
    /// Сохраняет JWT на устройстве.
    /// </summary>
    public Task SaveTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Токен не может быть пустым.",
                nameof(token));
        }

        return SecureStorage.Default.SetAsync(
            TokenKey,
            token);
    }

    /// <summary>
    /// Возвращает сохранённый JWT.
    /// </summary>
    public Task<string?> GetTokenAsync()
    {
        return SecureStorage.Default.GetAsync(TokenKey);
    }

    /// <summary>
    /// Удаляет JWT.
    /// Вызывается при выходе из аккаунта.
    /// </summary>
    public Task RemoveTokenAsync()
    {
        SecureStorage.Default.Remove(TokenKey);

        return Task.CompletedTask;
    }
}