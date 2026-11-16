using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Mobile.Views.Clubs;
using ClubPlatform.Mobile.Views.Admin;
using ClubPlatform.Mobile.Models;
using ClubPlatform.Shared.DTOs.Auth;
using ClubPlatform.Shared.DTOs.Users;
using ClubPlatform.Shared.Enums;
using System.Xml;
using Microsoft.Maui.Controls.Shapes;
using ClubPlatform.Mobile.Views.Support;
using ClubPlatform.Mobile.Views.Reports;

namespace ClubPlatform.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    private bool _isLoading;

    public ProfilePage(
        IApiService apiService,
        ITokenStorage tokenStorage)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadProfileAsync();
    }

    /// <summary>
    /// Получает актуальную информацию о текущем пользователе.
    /// </summary>
    private async Task LoadProfileAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем профиль...";

        try
        {
            // Сервер определяет пользователя по JWT-токену.
            var user = await _apiService.GetAsync<UserDto>(
                "api/Auth/me");

            if (user is null)
            {
                StatusLabel.Text = "Не удалось получить профиль.";
                return;
            }

            NameLabel.Text = user.Name;
            EmailLabel.Text = user.Email;

            AgeLabel.Text = user.Age > 0
                ? $"{user.Age} лет"
                : "Не указан";

            GenderLabel.Text = user.Gender switch
            {
                Gender.Male => "Мужской",
                Gender.Female => "Женский",
                Gender.Other => "Другой",
                _ => "Не указан"
            };

            RoleLabel.Text = user.Role switch
            {
                UserRole.Admin => "Администратор",
                _ => "Пользователь"
            };

            // Показываем административную панель только администратору.
            AdminPanelButton.IsVisible = user.Role == UserRole.Admin;

            // Разделы клубов и обращений доступны обычным пользователям.
            bool isAdmin = user.Role == UserRole.Admin;

            MyClubsSection.IsVisible = !isAdmin;
            UserActionsSection.IsVisible = !isAdmin;

            // Отображаем фотографию, если URL корректный.
            if (Uri.TryCreate(
                user.PhotoUrl,
                UriKind.Absolute,
                out var photoUri)
                && (photoUri.Scheme == Uri.UriSchemeHttp
                    || photoUri.Scheme == Uri.UriSchemeHttps))
            {
                ProfilePhoto.Source = ImageSource.FromUri(photoUri);
            }
            else
            {
                ProfilePhoto.Source = null;
            }
            // Загружаем клубы только для обычного пользователя.
            if (!isAdmin)
            {
                await LoadMyClubsAsync(user.Id);
            }

            StatusLabel.Text = "Профиль актуален.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить профиль.";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось получить данные пользователя.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Загружает и отображает клубы, в которых состоит текущий пользователь.
    /// </summary>
    private async Task LoadMyClubsAsync(Guid userId)
    {
        MyClubsLoadingIndicator.IsVisible = true;
        MyClubsLoadingIndicator.IsRunning = true;
        MyClubsEmptyLabel.IsVisible = false;

        MyClubsLayout.Children.Clear();

        try
        {
            // Получаем публичный профиль пользователя вместе со списком клубов.
            var profile = await _apiService.GetAsync<PublicUserProfileResponse>(
                $"api/users/{userId}/profile");

            if (profile?.Clubs == null || profile.Clubs.Count == 0)
            {
                MyClubsEmptyLabel.IsVisible = true;
                return;
            }

            // Создаём отдельную карточку для каждого клуба.

            foreach (var club in profile.Clubs)
            {
                // Создаём карточку клуба в фирменном стиле.
                var clubCard = new Border
                {
                    BackgroundColor = Color.FromArgb("#FFFFFF"),
                    Stroke = Color.FromArgb("#E8E2FA"),
                    StrokeThickness = 1,
                    Padding = 16,

                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = 18
                    },

                    Content = new VerticalStackLayout
                    {
                        Spacing = 8,

                        Children =
            {
                // Название клуба
                new Label
                {
                    Text = $"{club.Name}",
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#2512C9")
                },

                // Описание клуба
                new Label
                {
                    Text = club.Description,
                    FontSize = 13,
                    TextColor = Color.FromArgb("#746D8A"),
                    LineBreakMode = LineBreakMode.WordWrap
                },

                // Адрес клуба
                new Label
                {
                    Text = $"{club.Address}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6235F5")
                }
            }
                    }
                };

                MyClubsLayout.Children.Add(clubCard);
            }

        }
        catch (Exception ex)
        {
            MyClubsEmptyLabel.Text = "Не удалось загрузить список клубов.";
            MyClubsEmptyLabel.IsVisible = true;

            System.Diagnostics.Debug.WriteLine(
                $"Ошибка загрузки клубов пользователя: {ex.Message}");
        }
        finally
        {
            MyClubsLoadingIndicator.IsRunning = false;
            MyClubsLoadingIndicator.IsVisible = false;
        }
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadProfileAsync();
    }

    /// <summary>
    /// Завершает пользовательский сеанс.
    /// </summary>
    private async void OnLogoutClicked(
        object sender,
        EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Выход",
            "Выйти из текущего аккаунта?",
            "Выйти",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            // Удаляем сохранённый токен.
            await _tokenStorage.RemoveTokenAsync();

            // Убираем токен из HTTP-запросов.
            _apiService.SetBearerToken(null);

            // Возвращаем пользователя на приветственный экран.
            Application.Current!.MainPage =
                new NavigationPage(
                    new WelcomePage(
                        _apiService,
                        _tokenStorage));
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка выхода",
                ex.Message,
                "Понятно");
        }
    }

    /// <summary>
    /// Открывает форму редактирования профиля.
    /// </summary>
    private async void OnEditProfileClicked(
        object sender,
        EventArgs e)
    {
        var editPage = new EditProfilePage(_apiService);

        await Navigation.PushAsync(editPage);
    }

    /// <summary>
    /// Открывает главное меню административной панели.
    /// </summary>
    private async void OnAdminPanelClicked(
        object sender,
        EventArgs e)
    {
        var adminHomePage = new AdminHomePage(_apiService);

        await Navigation.PushAsync(adminHomePage);
    }

    /// <summary>
    /// Открывает страницу создания обращения в поддержку.
    /// </summary>
    private async void OnCreateSupportTicketClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new CreateSupportTicketPage(_apiService));
    }

    /// <summary>
    /// Открывает список обращений текущего пользователя.
    /// </summary>
    private async void OnMySupportTicketsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MySupportTicketsPage(_apiService));
    }

    /// <summary>
    /// Открывает список жалоб, отправленных текущим пользователем.
    /// </summary>
    private async void OnMyReportsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MyReportsPage(_apiService));
    }
}