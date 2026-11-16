namespace ClubPlatform.Api.Settings;

/// <summary>
/// Настройки JWT-токена.
/// Значения будут храниться в User Secrets,
/// а не непосредственно в коде.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Секретный ключ для подписи JWT.
    /// Должен быть длинным и случайным.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Издатель токена.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Аудитория токена.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни токена в минутах.
    /// </summary>
    public int ExpirationMinutes { get; set; }
}