using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.ClubSuggestions;

namespace ClubPlatform.Mobile.Views.Suggestions;

public partial class MyClubSuggestionsPage : ContentPage
{
    private readonly IApiService _apiService;
    private bool _isLoading;

    public MyClubSuggestionsPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSuggestionsAsync();
    }

    private async Task LoadSuggestionsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем предложения...";

        try
        {
            var suggestions =
                await _apiService.GetAsync<List<ClubSuggestionDto>>(
                    "api/ClubSuggestions/my")
                ?? new List<ClubSuggestionDto>();

            var items = suggestions
                .Select(item => new ClubSuggestionListItem
                {
                    Suggestion = item
                })
                .ToList();

            SuggestionsCollection.ItemsSource = items;

            StatusLabel.Text = items.Count switch
            {
                0 => "Предложений пока нет",
                1 => "Всего предложений: 1",
                _ => $"Всего предложений: {items.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить предложения.";

            await DisplayAlert(
                "Ошибка",
                $"Проверь подключение и авторизацию.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadSuggestionsAsync();
    }
}