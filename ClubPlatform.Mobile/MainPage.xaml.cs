using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;

namespace ClubPlatform.Mobile;

/// <summary>
/// Временная страница для проверки связи с Backend.
/// Позже заменим её настоящим экраном Welcome.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly IApiService _apiService;

    public MainPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    /// <summary>
    /// Выполняет тестовый запрос к API.
    /// </summary>
    private async void OnCheckApiClicked(
        object sender,
        EventArgs e)
    {
        CheckApiButton.IsEnabled = false;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        StatusLabel.Text = "Подключаемся к серверу...";

        try
        {
            var response =
                await _apiService.GetAsync<ApiHealthResponse>(
                    "api/health");

            if (response is null ||
                !string.Equals(
                    response.Status,
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                StatusLabel.Text =
                    "Сервер ответил, но статус не подтверждён.";

                return;
            }

            StatusLabel.Text =
                $"✅ Подключение успешно!\n\n" +
                $"{response.Message}\n\n" +
                $"Время сервера: {response.ServerTime:dd.MM.yyyy HH:mm:ss} UTC";
        }
        catch (HttpRequestException exception)
        {
            StatusLabel.Text =
                "❌ Не удалось подключиться к API.\n\n" +
                "Проверь, что Backend запущен и адрес API правильный.\n\n" +
                exception.Message;
        }
        catch (TaskCanceledException)
        {
            StatusLabel.Text =
                "❌ Превышено время ожидания ответа сервера. " +
                "Проверь подключение и запуск API.";
        }
        catch (Exception exception)
        {
            StatusLabel.Text =
                $"❌ Ошибка: {exception.Message}";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            CheckApiButton.IsEnabled = true;
        }
    }
}