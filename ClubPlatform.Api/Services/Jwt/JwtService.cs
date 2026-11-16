using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClubPlatform.Api.Models;
using ClubPlatform.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClubPlatform.Api.Services.Jwt;

/// <summary>
/// Реализация генерации JWT-токенов.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Создаёт JWT-токен для указанного пользователя.
    /// </summary>
    public string GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key))
        {
            throw new InvalidOperationException(
                "JWT Key не настроен.");
        }

        // Формируем список claims.
        var claims = new List<Claim>
        {
            // ID пользователя.
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            // ID пользователя для ClaimTypes.NameIdentifier.
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            // Email.
            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            // Имя пользователя.
            new(
                ClaimTypes.Name,
                user.Name),

            // Роль пользователя.
            new(
                ClaimTypes.Role,
                user.Role.ToString())
        };

        // Преобразуем секретную строку в массив байтов.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));

        // Используем HMAC SHA-256.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Время создания токена.
        var issuedAt = DateTime.UtcNow;

        // Время окончания действия токена.
        var expiresAt = issuedAt.AddMinutes(
            _settings.ExpirationMinutes);

        // Создаём токен.
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: credentials);

        // Преобразуем объект токена в строку.
        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}