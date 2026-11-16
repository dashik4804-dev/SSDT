
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Mobile.Views.Clubs;

namespace ClubPlatform.Mobile.Views;

public partial class ChatsPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    private bool _isLoading;

    public ChatsPage(
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

        await LoadChatsAsync();
    }

    /// <summary>
    /// Загружает клубы, чьи чаты доступны текущему пользователю.
    /// </summary>
    private async Task LoadChatsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем чаты...";

        try
        {
            var clubs =
                await _apiService.GetAsync<List<ClubListItem>>(
                    "api/Clubs")
                ?? new List<ClubListItem>();

            // Определяем роль пользователя по его профилю.
            var user =
                await _apiService.GetAsync<UserProfileResponse>(
                    "api/Auth/me");

            bool isAdmin = string.Equals(
                user?.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

            // Подпись зависит от роли текущего пользователя.
            SubtitleLabel.Text = isAdmin
                ? "Чаты клубов для просмотра и модерации"
                : "Переписки клубов, в которых Вы участвуете";

            // Обычному пользователю показываем только его клубы.
            // Администратор может открыть чат любого клуба,
            // используя предусмотренные сервером права.
            var availableChats = isAdmin
                ? clubs
                : clubs.Where(club => club.IsMember).ToList();

            ChatsCollection.ItemsSource = availableChats;

            StatusLabel.Text = availableChats.Count switch
            {
                0 => "Доступных чатов нет",
                1 => "Доступен 1 чат",
                _ => $"Доступно чатов: {availableChats.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить чаты";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось получить список клубов.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Открывает переписку выбранного клуба.
    /// </summary>
    private async void OnClubTapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border
            || border.BindingContext is not ClubListItem club)
        {
            return;
        }

        var chatPage = new ChatPage(
            _apiService,
            _tokenStorage,
            club);

        await Navigation.PushAsync(chatPage);
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadChatsAsync();
    }
}
