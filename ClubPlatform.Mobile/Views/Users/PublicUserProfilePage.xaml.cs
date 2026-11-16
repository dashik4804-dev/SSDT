
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using Microsoft.Maui.Controls.Shapes;

namespace ClubPlatform.Mobile.Views.Users;

public partial class PublicUserProfilePage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly Guid _userId;

    private bool _isLoading;

    public PublicUserProfilePage(
        IApiService apiService,
        Guid userId)
    {
        InitializeComponent();

        _apiService = apiService;
        _userId = userId;
    }

    // Загружаем профиль при открытии страницы
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isLoading)
        {
            await LoadProfileAsync();
        }
    }

    // Получаем публичный профиль пользователя с сервера
    private async Task LoadProfileAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var profile =
                await _apiService.GetAsync<PublicUserProfileResponse>(
                    $"api/users/{_userId}/profile");

            if (profile?.User == null)
            {
                await DisplayAlert(
                    "Ошибка",
                    "Не удалось найти профиль пользователя.",
                    "ОК");

                return;
            }

            DisplayProfile(profile);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось загрузить профиль пользователя.\n{ex.Message}",
                "ОК");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            _isLoading = false;
        }
    }

    // Отображаем информацию о пользователе

    private void DisplayProfile(PublicUserProfileResponse profile)
    {
        // Имя пользователя
        UserNameLabel.Text = profile.User.Name;

        // Возраст
        UserAgeLabel.Text = $"{profile.User.Age} лет";

        // Пол
        UserGenderLabel.Text =
            GetGenderText(profile.User.Gender);

        // Фотография
        if (!string.IsNullOrWhiteSpace(profile.User.PhotoUrl) &&
            Uri.TryCreate(
                profile.User.PhotoUrl,
                UriKind.Absolute,
                out var photoUri))
        {
            UserPhoto.Source = ImageSource.FromUri(photoUri);
        }
        else
        {
            UserPhoto.Source = null;
        }

        // Очищаем список клубов
        ClubsLayout.Children.Clear();

        var clubs = profile.Clubs ??
            new List<PublicUserClubItem>();

        // Показываем пустое состояние при отсутствии клубов
        bool hasNoClubs = clubs.Count == 0;

        ClubsEmptyLabel.IsVisible = hasNoClubs;
        ClubsEmptyBorder.IsVisible = hasNoClubs;

        // Создаём красивые карточки клубов
        foreach (var club in clubs)
        {
            var clubCard = new Border
            {
                BackgroundColor = Colors.White,

                Stroke = Color.FromArgb("#E8E2FA"),

                StrokeThickness = 1,

                Padding = 16,

                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 18
                },

                Content = new VerticalStackLayout
                {
                    Spacing = 10,

                    Children =
                {
                    // Название клуба
                    new Label
                    {
                        Text = club.Name,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#2512C9"),
                        LineBreakMode = LineBreakMode.WordWrap
                    },

                    // Описание
                    new Label
                    {
                        Text = club.Description,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#746D8A"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        MaxLines = 4
                    },

                    // Адрес
                    new Border
                    {
                        BackgroundColor =
                            Color.FromArgb("#EEE8FF"),

                        StrokeThickness = 0,

                        Padding = new Thickness(12, 9),

                        StrokeShape = new RoundRectangle
                        {
                            CornerRadius = 12
                        },

                        Content = new Label
                        {
                            Text = club.Address,
                            FontSize = 12,
                            TextColor = Color.FromArgb("#6235F5"),
                            LineBreakMode = LineBreakMode.WordWrap
                        }
                    }
                }
                }
            };

            ClubsLayout.Children.Add(clubCard);
        }
    }


    // Переводим значение пола в понятный пользователю текст
    private string GetGenderText(string gender)
    {
        return gender switch
        {
            "Male" => "Мужчина",
            "Female" => "Женщина",
            _ => gender
        };
    }
}
