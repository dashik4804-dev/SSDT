using ClubPlatform.Shared.DTOs.Auth;

namespace ClubPlatform.Api.Services.Auth;

/// <summary>
/// Сервис регистрации и авторизации.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет вход существующего пользователя.
    /// </summary>
    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}