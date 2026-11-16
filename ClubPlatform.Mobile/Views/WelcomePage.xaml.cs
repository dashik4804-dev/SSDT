using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;

namespace ClubPlatform.Mobile.Views;

/// <summary>
/// Стартовая страница приложения.
/// </summary>
public partial class WelcomePage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    public WelcomePage(
        IApiService apiService,
        ITokenStorage tokenStorage)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Переходит к форме входа.
    /// </summary>
    private async void OnLoginClicked(
        object sender,
        EventArgs e)
    {
        var loginPage = new LoginPage(
            _apiService,
            _tokenStorage);

        await Navigation.PushAsync(loginPage);
    }

    /// <summary>
    /// Открывает экран регистрации.
    /// </summary>
    private async void OnRegisterClicked(
        object sender,
        EventArgs e)
    {
        var registerPage = new RegisterPage(
            _apiService,
            _tokenStorage);

        await Navigation.PushAsync(registerPage);
    }
}