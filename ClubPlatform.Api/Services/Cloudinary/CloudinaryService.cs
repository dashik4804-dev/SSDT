using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ClubPlatform.Api.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

// Псевдоним для класса Cloudinary из библиотеки.
// Нужен потому, что наш namespace тоже называется Cloudinary.
using CloudinaryClient = CloudinaryDotNet.Cloudinary;

namespace ClubPlatform.Api.Services.Cloudinary;

/// <summary>
/// Реализация загрузки изображений в Cloudinary.
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly CloudinaryClient _cloudinary;

    public CloudinaryService(
        IOptions<CloudinarySettings> settings)
    {
        var configuration = settings.Value;

        // -----------------------------------------------------
        // Проверяем настройки Cloudinary.
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(configuration.CloudName))
        {
            throw new InvalidOperationException(
                "Cloudinary CloudName не настроен.");
        }

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            throw new InvalidOperationException(
                "Cloudinary ApiKey не настроен.");
        }

        if (string.IsNullOrWhiteSpace(configuration.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary ApiSecret не настроен.");
        }

        // Создаём учётные данные Cloudinary.
        var account = new Account(
            configuration.CloudName,
            configuration.ApiKey,
            configuration.ApiSecret);

        // Создаём клиент Cloudinary.
        _cloudinary = new CloudinaryClient(account);

        // Используем безопасные HTTPS-ссылки.
        _cloudinary.Api.Secure = true;
    }

    /// <summary>
    /// Загружает изображение в Cloudinary
    /// и возвращает HTTPS URL.
    /// </summary>
    public async Task<string> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        // -----------------------------------------------------
        // Проверяем файл.
        // -----------------------------------------------------

        if (file is null || file.Length == 0)
        {
            throw new ArgumentException(
                "Файл изображения пустой.");
        }

        // -----------------------------------------------------
        // Разрешённые форматы.
        // -----------------------------------------------------

        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedContentTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Разрешены только JPEG, PNG и WebP изображения.");
        }

        // -----------------------------------------------------
        // Ограничение размера.
        // Максимум 10 МБ.
        // -----------------------------------------------------

        const long maxFileSize =
            10 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            throw new ArgumentException(
                "Размер изображения не должен превышать 10 МБ.");
        }

        // -----------------------------------------------------
        // Копируем файл в память.
        // -----------------------------------------------------

        await using var stream =
            new MemoryStream();

        await file.CopyToAsync(
            stream,
            cancellationToken);

        stream.Position = 0;

        // -----------------------------------------------------
        // Параметры загрузки в Cloudinary.
        // -----------------------------------------------------

        var uploadParams =
            new ImageUploadParams
            {
                File = new FileDescription(
                    file.FileName,
                    stream),

                // Все фотографии пользователей
                // складываем в эту папку.
                Folder = folder,

                // Cloudinary создаст уникальный ID.
                UseFilename = false,

                UniqueFilename = true
            };

        // -----------------------------------------------------
        // Загружаем изображение.
        // -----------------------------------------------------

        var uploadResult =
            await _cloudinary.UploadAsync(uploadParams);

        // Проверяем ответ Cloudinary.
        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException(
                $"Ошибка Cloudinary: " +
                $"{uploadResult.Error.Message}");
        }

        // Возвращаем HTTPS URL.
        return uploadResult.SecureUrl.ToString();
    }
}