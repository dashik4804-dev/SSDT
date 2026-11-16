using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClubPlatform.Mobile.Services.Api;

/// <summary>
/// Реализация HTTP-запросов к ASP.NET Core API.
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    // Общие настройки чтения и записи JSON.
    // Enum преобразуются в строки: "User", "Female" и т. д.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Выполняет GET-запрос.
    /// </summary>
    public async Task<T?> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<T>(
            endpoint,
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет POST-запрос с JSON-телом.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"API вернул {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. Ответ: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// Отправляет файл на сервер и читает JSON-ответ.
    /// </summary>
    public async Task<TResponse?> PostFileAsync<TResponse>(
        string endpoint,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();

        // Создаём часть запроса, содержащую файл.
        var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        // Имя "file" должно совпадать с параметром IFormFile file
        // в ImagesController.
        form.Add(fileContent, "file", fileName);

        using var response = await _httpClient.PostAsync(
            endpoint,
            form,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"API вернул {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. Ответ: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// Устанавливает или удаляет JWT в заголовках запросов.
    /// </summary>
    public void SetBearerToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }


    /// <summary>
    /// Отправляет PUT-запрос и десериализует ответ сервера.
    /// </summary>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            endpoint,
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// Выполняет PUT-запрос без тела.
    /// Используется, например, для включения или снятия ограничения.
    /// </summary>
    public async Task PutAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync(
            endpoint,
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Выполняет DELETE-запрос.
    /// </summary>
    public async Task DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            endpoint,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

}