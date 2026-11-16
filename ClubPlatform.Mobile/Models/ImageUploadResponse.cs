namespace ClubPlatform.Mobile.Models;

/// <summary>
/// Ответ сервера после загрузки изображения в Cloudinary.
/// </summary>
public class ImageUploadResponse
{
    public string Url { get; set; } = string.Empty;
}