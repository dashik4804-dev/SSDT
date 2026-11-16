using Microsoft.AspNetCore.Http;

namespace ClubPlatform.Api.Services.Cloudinary;

/// <summary>
/// Сервис загрузки изображений в Cloudinary.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Загружает изображение и возвращает его HTTPS URL.
    /// </summary>
    Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default);
}