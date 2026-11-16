
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Reports;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class MutedUsersPage : ContentPage
{
    private readonly IApiService _apiService;

    // Список клубов для выбора.
    private List<ClubListItem> _clubs = new();

    private bool _isLoading;
    private bool _isClubsLoaded;

    // true — показываем пользователей с запретом писать.
    // false — показываем удалённых из клуба.
    private bool _showMutedUsers = true;

    public MutedUsersPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isClubsLoaded)
            await LoadClubsAsync();
    }

    /// <summary>
    /// Загружает список клубов для выбора.
    /// </summary>
    private async Task LoadClubsAsync()
    {
        try
        {
            StatusLabel.Text = "Загружаем клубы...";

            _clubs = await _apiService.GetAsync<List<ClubListItem>>(
                "api/Clubs") ?? new List<ClubListItem>();

            ClubPicker.ItemsSource = _clubs;
            _isClubsLoaded = true;

            if (_clubs.Count == 0)
            {
                StatusLabel.Text = "Клубов пока нет.";
                return;
            }

            // При первом открытии выбираем первый клуб.
            ClubPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить клубы.";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось получить список клубов.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Загружает пользователей с запретом на сообщения.
    /// </summary>
    private async Task LoadMutedUsersAsync()
    {
        if (_isLoading)
            return;

        if (ClubPicker.SelectedItem is not ClubListItem club)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем ограничения...";

        try
        {
            var users =
                await _apiService.GetAsync<List<MutedClubMemberDto>>(
                    "api/Reports/muted-users")
                ?? new List<MutedClubMemberDto>();

            // Показываем только ограничения выбранного клуба.
            var clubUsers = users
                .Where(user => user.ClubId == club.Id)
                .ToList();

            MutedUsersCollection.ItemsSource = clubUsers;

            StatusLabel.Text = clubUsers.Count switch
            {
                0 => "В этом клубе нет пользователей с запретом писать.",
                1 => "Запрет на сообщения действует для 1 пользователя.",
                _ => $"Пользователей с запретом писать: {clubUsers.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить ограничения.";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось получить список пользователей.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Загружает пользователей, удалённых из выбранного клуба.
    /// </summary>
    private async Task LoadRemovedUsersAsync()
    {
        if (_isLoading)
            return;

        if (ClubPicker.SelectedItem is not ClubListItem club)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем удалённых пользователей...";

        try
        {
            var users =
                await _apiService.GetAsync<List<BannedMemberItem>>(
                    $"api/clubs/{club.Id}/members/bans")
                ?? new List<BannedMemberItem>();

            RemovedUsersCollection.ItemsSource = users;

            StatusLabel.Text = users.Count switch
            {
                0 => "В этом клубе нет удалённых пользователей.",
                1 => "Удалён 1 пользователь.",
                _ => $"Удалённых пользователей: {users.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить удалённых пользователей.";

            await DisplayAlert(
                "Ошибка",
                $"Не удалось получить список удалённых пользователей.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Переключает страницу на пользователей с запретом писать.
    /// </summary>
    private async void OnMutedTabClicked(
        object sender,
        EventArgs e)
    {
        _showMutedUsers = true;

        MutedUsersCollection.IsVisible = true;
        RemovedUsersCollection.IsVisible = false;

        MutedTabButton.BackgroundColor =
            Color.FromArgb("#6235F5");
        MutedTabButton.TextColor = Colors.White;

        RemovedTabButton.BackgroundColor =
            Color.FromArgb("#EEE8FF");
        RemovedTabButton.TextColor =
            Color.FromArgb("#6235F5");

        await LoadMutedUsersAsync();
    }


    /// <summary>
    /// Переключает страницу на удалённых пользователей.
    /// </summary>
    private async void OnRemovedTabClicked(
        object sender,
        EventArgs e)
    {
        _showMutedUsers = false;

        MutedUsersCollection.IsVisible = false;
        RemovedUsersCollection.IsVisible = true;

        RemovedTabButton.BackgroundColor =
            Color.FromArgb("#6235F5");
        RemovedTabButton.TextColor = Colors.White;

        MutedTabButton.BackgroundColor =
            Color.FromArgb("#EEE8FF");
        MutedTabButton.TextColor =
            Color.FromArgb("#6235F5");

        await LoadRemovedUsersAsync();
    }


    /// <summary>
    /// Загружает соответствующий список после выбора клуба.
    /// </summary>
    private async void OnClubPickerSelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (_showMutedUsers)
            await LoadMutedUsersAsync();
        else
            await LoadRemovedUsersAsync();
    }

    /// <summary>
    /// Снимает запрет на отправку сообщений.
    /// Пользователь остаётся участником клуба.
    /// </summary>
    private async void OnUnmuteClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not MutedClubMemberDto member)
        {
            return;
        }

        bool confirmed = await DisplayAlert(
            "Разрешить писать?",
            $"Снять запрет на отправку сообщений для пользователя "
            + $"«{member.UserName}» в клубе «{member.ClubName}»?",
            "Разрешить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            var endpoint =
                $"api/Reports/clubs/{member.ClubId}"
                + $"/users/{member.UserId}/mute?muted=false";

            await _apiService.PutAsync(endpoint);

            await DisplayAlert(
                "Готово",
                $"Пользователю «{member.UserName}» снова разрешено писать.",
                "Понятно");

            await LoadMutedUsersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось снять запрет.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Снимает блокировку с пользователя, удалённого из клуба.
    /// После этого он сможет снова вступить в клуб.
    /// </summary>
    private async void OnUnbanClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not BannedMemberItem member)
        {
            return;
        }

        if (ClubPicker.SelectedItem is not ClubListItem club)
            return;

        bool confirmed = await DisplayAlert(
            "Разблокировать пользователя?",
            $"Снять блокировку с пользователя «{member.UserName}» "
            + $"в клубе «{club.Name}»?\n\n"
            + "После этого он сможет снова вступить в клуб.",
            "Разблокировать",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            await _apiService.DeleteAsync(
                $"api/clubs/{club.Id}/members/bans/{member.UserId}");

            await DisplayAlert(
                "Готово",
                $"Пользователь «{member.UserName}» разблокирован.",
                "Понятно");

            await LoadRemovedUsersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось снять блокировку.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Обновляет открытый в данный момент список.
    /// </summary>
    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        if (_showMutedUsers)
            await LoadMutedUsersAsync();
        else
            await LoadRemovedUsersAsync();
    }
}
