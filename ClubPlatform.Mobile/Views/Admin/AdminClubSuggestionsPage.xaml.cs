using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.ClubSuggestions;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class AdminClubSuggestionsPage : ContentPage
{
    private readonly IApiService _apiService;
    private bool _isLoading;

    public AdminClubSuggestionsPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSuggestionsAsync();
    }

    /// <summary>
    /// Загружает все предложения, доступные администратору.
    /// </summary>
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
                    "api/ClubSuggestions")
                ?? new List<ClubSuggestionDto>();

            var items = suggestions
                .OrderBy(item =>
                    item.Status == "Pending" ? 0 : 1)
                .ThenByDescending(item => item.CreatedAt)
                .Select(item => new ClubSuggestionListItem
                {
                    Suggestion = item
                })
                .ToList();

            SuggestionsCollection.ItemsSource = items;

            var pendingCount = items.Count(item => item.CanReview);

            StatusLabel.Text =
                $"Всего: {items.Count}. На рассмотрении: {pendingCount}.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить предложения.";

            await DisplayAlert(
                "Ошибка",
                $"Проверь права администратора и подключение.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnApproveClicked(
        object sender,
        EventArgs e)
    {
        await ReviewAsync(sender, "Approved");
    }

    private async void OnRejectClicked(
        object sender,
        EventArgs e)
    {
        await ReviewAsync(sender, "Rejected");
    }

    /// <summary>
    /// Сохраняет решение администратора и необязательный комментарий.
    /// </summary>
    private async Task ReviewAsync(
        object sender,
        string status)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not ClubSuggestionListItem item)
        {
            return;
        }

        if (!item.CanReview)
        {
            await DisplayAlert(
                "Предложение уже рассмотрено",
                "Обнови список, чтобы увидеть актуальный статус.",
                "Понятно");

            return;
        }

        var isApproved = status == "Approved";

        var comment = await DisplayPromptAsync(
            isApproved ? "Одобрение предложения" : "Отклонение предложения",
            isApproved
                ? "Можешь оставить комментарий автору (необязательно)."
                : "Укажи причину отказа или оставь комментарий (необязательно).",
            accept: isApproved ? "Одобрить" : "Отклонить",
            cancel: "Отмена",
            maxLength: 1000,
            placeholder: "Комментарий администратора");

        // Cancel возвращает null — решение не отправляем.
        if (comment is null)
            return;

        try
        {
            var request = new ReviewClubSuggestionRequest
            {
                Status = status,
                AdminComment = string.IsNullOrWhiteSpace(comment)
                    ? null
                    : comment.Trim()
            };

            await _apiService.PutAsync<
                ReviewClubSuggestionRequest,
                ClubSuggestionDto>(
                    $"api/ClubSuggestions/{item.Id}/review",
                    request);

            await LoadSuggestionsAsync();

            await DisplayAlert(
                "Готово",
                isApproved
                    ? "Предложение одобрено. Теперь можно перейти в «Управление клубами» и создать клуб на его основе."
                    : "Предложение отклонено. Решение сохранено.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось сохранить решение.\n\n{ex.Message}",
                "Понятно");
        }
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadSuggestionsAsync();
    }

    /// <summary>
    /// Открывает форму создания клуба с данными из предложения.
    /// </summary>
    private async void OnCreateClubClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not ClubSuggestionListItem item)
        {
            return;
        }

        if (!item.CanCreateClub)
            return;

        var editor = new ClubEditorPage(
            _apiService,
            suggestion: item.Suggestion);

        await Navigation.PushAsync(editor);
    }
}