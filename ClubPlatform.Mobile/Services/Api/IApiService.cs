namespace ClubPlatform.Mobile.Services.Api;

/// <summary>
/// Общий интерфейс для HTTP-запросов к Backend.
/// </summary>
public interface IApiService
{
    /// <summary>
    /// Выполняет GET-запрос и преобразует JSON в C#-объект.
    /// </summary>
    Task<T?> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет POST-запрос с JSON-телом.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает файл через multipart/form-data.
    /// </summary>
    Task<TResponse?> PostFileAsync<TResponse>(
        string endpoint,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task PutAsync(
    string endpoint,
    CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Устанавливает JWT для последующих запросов.
    /// Если token равен null или пуст, авторизация сбрасывается.
    /// </summary>
    void SetBearerToken(string? token);
}