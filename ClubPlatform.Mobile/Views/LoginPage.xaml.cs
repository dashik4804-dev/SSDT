using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Shared.DTOs.Auth;

namespace ClubPlatform.Mobile.Views;

/// <summary>
/// Страница входа в ClubPlatform.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    private bool _isProcessing;

    public LoginPage(
        IApiService apiService,
        ITokenStorage tokenStorage)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Выполняет вход через Backend.
    /// </summary>
    private async void OnLoginClicked(
        object? sender,
        EventArgs e)
    {
        // Не допускаем повторную отправку формы.
        if (_isProcessing)
        {
            return;
        }

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        // Проверяем заполнение полей.
        if (string.IsNullOrWhiteSpace(email))
        {
            StatusLabel.Text = "Введи email.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Введи пароль.";
            return;
        }

        _isProcessing = true;

        LoginButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatusLabel.Text = "Выполняется вход...";

        try
        {
            // Отправляем email и пароль на наш API.
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response =
                await _apiService.PostAsync<LoginRequest, AuthResponse>(
                    "api/Auth/login",
                    request);

            if (response is null ||
                string.IsNullOrWhiteSpace(response.Token))
            {
                StatusLabel.Text =
                    "Сервер не вернул токен авторизации.";

                return;
            }

            // Сохраняем токен в защищённом хранилище Android.
            await _tokenStorage.SaveTokenAsync(response.Token);

            // Устанавливаем токен для дальнейших API-запросов.
            _apiService.SetBearerToken(response.Token);

            // После успешного входа открываем каталог клубов.
            Application.Current!.MainPage =
                new MainTabsPage(_apiService, _tokenStorage);

            StatusLabel.Text = "Вход выполнен успешно.";

            // Показываем роль, полученную от сервера.
            // Пока экран клубов ещё не создан.
            //await DisplayAlert(
            //    "Добро пожаловать!",
            //    $"Привет, {response.User.Name}!\n\n" +
            //    $"Роль: {response.User.Role}\n\n" +
            //    "Авторизация прошла успешно. " +
            //    "Сейчас мы завершим остальные экраны приложения.",
            //    "Продолжить");
        }
        catch (HttpRequestException exception)
        {
            // API возвращает подробную информацию об HTTP-ошибке.
            // Например, 401 означает неправильный email или пароль.
            if (exception.Message.Contains("401"))
            {
                StatusLabel.Text = "Неверный email или пароль.";
            }
            else
            {
                StatusLabel.Text =
                    "Не удалось выполнить вход. Проверь подключение к серверу.";

                System.Diagnostics.Debug.WriteLine(exception.Message);
            }
        }
        catch (Exception exception)
        {
            StatusLabel.Text =
                "Произошла ошибка при входе. Попробуй ещё раз.";

            System.Diagnostics.Debug.WriteLine(exception.ToString());
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            LoginButton.IsEnabled = true;

            _isProcessing = false;
        }
    }

    /// <summary>
    /// Возвращает пользователя на Welcome.
    /// </summary>
    private async void OnBackClicked(
        object? sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}