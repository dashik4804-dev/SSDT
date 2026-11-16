using ClubPlatform.Api.Models;

namespace ClubPlatform.Api.Services.Jwt;

/// <summary>
/// Сервис генерации JWT-токенов.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Создаёт JWT для пользователя.
    /// </summary>
    string GenerateToken(User user);
}