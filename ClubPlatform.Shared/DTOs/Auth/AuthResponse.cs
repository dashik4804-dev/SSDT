using ClubPlatform.Shared.DTOs.Users;

namespace ClubPlatform.Shared.DTOs.Auth;

/// <summary>
/// Ответ API после успешной авторизации.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT-токен.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время окончания действия токена.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Данные авторизованного пользователя.
    /// </summary>
    public UserDto User { get; set; } = null!;
}