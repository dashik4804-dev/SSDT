namespace ClubPlatform.Api.Settings;

/// <summary>
/// Настройки подключения к Cloudinary.
/// Секретные значения загружаются из User Secrets.
/// </summary>
public class CloudinarySettings
{
    /// <summary>
    /// Название Cloudinary Cloud.
    /// </summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>
    /// Публичный API Key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Секретный API Secret.
    /// Никогда не должен попадать в исходный код.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;
}