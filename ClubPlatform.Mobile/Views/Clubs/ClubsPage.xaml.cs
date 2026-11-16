using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Mobile.Views;
using ClubPlatform.Mobile.Views.Suggestions;

namespace ClubPlatform.Mobile.Views.Clubs;

public partial class ClubsPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;
    private bool _roleChecked;

    private List<ClubListItem> _allClubs = new();
    private bool _isLoading;
    // Выбранные интересы. Несколько ID означают фильтр по нескольким интересам.
    private readonly HashSet<Guid> _selectedInterestIds = new();

    // Кнопки интересов, чтобы менять их оформление при выборе.
    private readonly Dictionary<Guid, Button> _interestFilterButtons = new();

    public ClubsPage(
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

        // Сначала определяем роль, чтобы показать нужные кнопки.
        if (!_roleChecked)
        {
            await UpdateSuggestionButtonsVisibilityAsync();
        }

        // Загружаем каталог, только если он ещё не загружен.
        if (_allClubs.Count == 0)
        {
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Загружает клубы и интересы с сервера.
    /// </summary>
    private async Task LoadDataAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatusLabel.Text = "Загружаем каталог...";

        try
        {
            // Получаем список клубов.
            _allClubs =
                await _apiService.GetAsync<List<ClubListItem>>(
                    "api/Clubs")
                ?? new List<ClubListItem>();

            var interests =
                await _apiService.GetAsync<List<InterestListItem>>(
                    "api/Interests")
                ?? new List<InterestListItem>();
            // Получаем роль текущего пользователя с сервера.
            var currentUser =
                await _apiService.GetAsync<UserProfileResponse>(
                    "api/Auth/me");

            bool isAdmin = string.Equals(
                currentUser?.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

            // Предложения доступны только обычным пользователям.
            SuggestClubButton.IsVisible = !isAdmin;
            MySuggestionsButton.IsVisible = !isAdmin;

            BuildInterestFilters(interests);

            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить клубы";

            await DisplayAlert(
                "Ошибка загрузки",
                $"Не удалось получить данные от сервера.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
        }
    }

    /// <summary>
    /// Создаёт кнопки для выбора одного или нескольких интересов.
    /// </summary>
    /// <summary>
    /// Создаёт компактные кнопки для выбора интересов.
    /// </summary>
    private void BuildInterestFilters(List<InterestListItem> interests)
    {
        // Очищаем старые кнопки перед обновлением списка.
        InterestFiltersLayout.Children.Clear();
        _interestFilterButtons.Clear();

        // Оставляем только те выбранные интересы,
        // которые по-прежнему существуют на сервере.
        var validIds = interests
            .Select(item => item.Id)
            .ToHashSet();

        _selectedInterestIds.RemoveWhere(
            id => !validIds.Contains(id));

        // Создаём кнопку для каждого интереса.
        foreach (var interest in interests)
        {
            var option = interest;

            // Настраиваем компактную кнопку интереса.
            var button = new Button
            {
                Text = option.DisplayName,
                Padding = new Thickness(10, 6),
                FontSize = 12,
                CornerRadius = 18,
                HeightRequest = 36,
                Margin = new Thickness(0)
            };

            // При нажатии включаем или выключаем фильтр.
            button.Clicked += (_, _) =>
                ToggleInterestFilter(option.Id);

            // Сохраняем кнопку, чтобы менять её цвет.
            _interestFilterButtons[option.Id] = button;

            // Добавляем кнопку в горизонтальный список.
            InterestFiltersLayout.Children.Add(button);
        }

        // Отображаем правильные цвета выбранных фильтров.
        UpdateInterestFilterButtons();
    }

    /// <summary>
    /// Переключает выбранный интерес.
    /// </summary>
    private void ToggleInterestFilter(Guid interestId)
    {
        if (!_selectedInterestIds.Add(interestId))
        {
            _selectedInterestIds.Remove(interestId);
        }

        UpdateInterestFilterButtons();
        ApplyFilters();
    }

    /// <summary>
    /// Сбрасывает фильтр и возвращает все клубы.
    /// </summary>
    private void OnAllInterestsClicked(object? sender, EventArgs e)
    {
        _selectedInterestIds.Clear();

        UpdateInterestFilterButtons();
        ApplyFilters();
    }


    /// <summary>
    /// Обновляет оформление фильтров интересов.
    /// Выбранные интересы выделяются фиолетовым.
    /// </summary>
    private void UpdateInterestFilterButtons()
    {
        bool hasNoFilter = _selectedInterestIds.Count == 0;

        // Кнопка "Все интересы".
        AllInterestsButton.BackgroundColor = hasNoFilter
            ? Color.FromArgb("#6235F5")
            : Color.FromArgb("#EEE8FF");

        AllInterestsButton.TextColor = hasNoFilter
            ? Colors.White
            : Color.FromArgb("#6235F5");

        // Кнопки отдельных интересов.
        foreach (var item in _interestFilterButtons)
        {
            bool isSelected =
                _selectedInterestIds.Contains(item.Key);

            item.Value.BackgroundColor = isSelected
                ? Color.FromArgb("#6235F5")
                : Color.FromArgb("#EEE8FF");

            item.Value.TextColor = isSelected
                ? Colors.White
                : Color.FromArgb("#6235F5");
        }
    }


    /// <summary>
    /// Применяет поиск и выбранный фильтр интересов.
    /// </summary>
    private void ApplyFilters()
    {
        IEnumerable<ClubListItem> result = _allClubs;

        var searchText = SearchBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            result = result.Where(club =>
                (club.Name?.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                (club.Description?.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                (club.Address?.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // Если выбрано несколько интересов,
        // показываем клубы, в которых есть хотя бы один из них.
        if (_selectedInterestIds.Count > 0)
        {
            result = result.Where(club =>
                club.Interests.Any(interest =>
                    _selectedInterestIds.Contains(interest.Id)));
        }

        var filteredClubs = result.ToList();

        ClubsCollection.ItemsSource = filteredClubs;

        StatusLabel.Text = filteredClubs.Count switch
        {
            0 => "Клубы не найдены",
            1 => "Найден 1 клуб",
            2 or 3 or 4 => $"Найдено {filteredClubs.Count} клуба",
            _ => $"Найдено клубов: {filteredClubs.Count}"
        };
    }

    private void OnSearchTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void OnInterestChanged(
        object sender,
        EventArgs e)
    {
        ApplyFilters();
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadDataAsync();
    }
    /// <summary>
    /// Открывает подробную информацию о выбранном клубе.
    /// </summary>
    private async void OnClubTapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (border.BindingContext is not ClubListItem club)
            return;

        var detailsPage = new ClubDetailsPage(
            _apiService,
            _tokenStorage,
            club);

        await Navigation.PushAsync(detailsPage);
    }

    /// <summary>
    /// Открывает профиль текущего пользователя.
    /// </summary>
    private async void OnProfileClicked(
        object sender,
        EventArgs e)
    {
        var profilePage = new ProfilePage(
            _apiService,
            _tokenStorage);

        await Navigation.PushAsync(profilePage);
    }

    /// <summary>
    /// Открывает форму отправки новой идеи.
    /// </summary>
    private async void OnSuggestClubClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new SuggestClubPage(_apiService));
    }

    /// <summary>
    /// Открывает личный список предложений пользователя.
    /// </summary>
    private async void OnMySuggestionsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MyClubSuggestionsPage(_apiService));
    }

    /// <summary>
    /// Показывает кнопки предложений только обычным пользователям.
    /// </summary>
    /// <summary>
    /// Показывает предложения обычному пользователю,
    /// скрывает их у администратора.
    /// </summary>
    private async Task UpdateSuggestionButtonsVisibilityAsync()
    {
        // По умолчанию показываем обе кнопки.
        SuggestClubButton.IsVisible = true;
        MySuggestionsButton.IsVisible = true;

        try
        {
            var currentUser =
                await _apiService.GetAsync<UserProfileResponse>(
                    "api/Auth/me");

            if (currentUser is null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Не удалось получить профиль пользователя.");

                return;
            }

            bool isAdmin = string.Equals(
                currentUser.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

            // Администратору эти кнопки не нужны.
            SuggestClubButton.IsVisible = !isAdmin;
            MySuggestionsButton.IsVisible = !isAdmin;

            _roleChecked = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Ошибка проверки роли: {ex}");

            // При ошибке загрузки профиля не скрываем кнопки.
            SuggestClubButton.IsVisible = true;
            MySuggestionsButton.IsVisible = true;
        }
    }
}