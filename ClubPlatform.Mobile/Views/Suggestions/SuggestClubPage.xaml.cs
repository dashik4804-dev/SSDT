
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.ClubSuggestions;

namespace ClubPlatform.Mobile.Views.Suggestions;

public partial class SuggestClubPage : ContentPage
{
    private readonly IApiService _apiService;

    private bool _interestsLoaded;
    private bool _isSubmitting;

    public SuggestClubPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    /// <summary>
    /// Вариант интереса для выпадающего списка.
    /// </summary>
    private sealed class InterestPickerOption
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsOther { get; set; }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_interestsLoaded)
        {
            await LoadInterestsAsync();
        }
    }

    /// <summary>
    /// Загружает существующие интересы и добавляет вариант «Другое».
    /// </summary>
    private async Task LoadInterestsAsync()
    {
        try
        {
            var interests =
                await _apiService.GetAsync<List<InterestListItem>>(
                    "api/Interests")
                ?? new List<InterestListItem>();

            var options = interests.Select(interest =>
                new InterestPickerOption
                {
                    Name = interest.Name,
                    DisplayName = interest.DisplayName,
                    IsOther = false
                })
                .ToList();

            // Добавляем специальный вариант для нового интереса.
            options.Add(new InterestPickerOption
            {
                Name = string.Empty,
                DisplayName = "Другое",
                IsOther = true
            });

            InterestPicker.ItemsSource = options;

            InterestPicker.ItemDisplayBinding =
                new Binding(nameof(InterestPickerOption.DisplayName));

            _interestsLoaded = true;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить интересы.";

            await DisplayAlert(
                "Ошибка",
                $"Можно повторить загрузку позже.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Показывает поле ввода, если пользователь выбрал «Другое».
    /// </summary>
    private void OnInterestSelectionChanged(
        object sender,
        EventArgs e)
    {
        var selected =
            InterestPicker.SelectedItem as InterestPickerOption;

        bool isOther = selected?.IsOther == true;

        OtherInterestLayout.IsVisible = isOther;

        if (!isOther)
        {
            OtherInterestEntry.Text = string.Empty;
        }
    }

    private async void OnSubmitClicked(
        object sender,
        EventArgs e)
    {
        if (_isSubmitting)
            return;

        var name = NameEntry.Text?.Trim();
        var description = DescriptionEditor.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            await DisplayAlert(
                "Проверь название",
                "Название должно содержать не менее двух символов.",
                "Понятно");

            return;
        }

        if (string.IsNullOrWhiteSpace(description)
            || description.Length < 5
            || description.Length > 2000)
        {
            await DisplayAlert(
                "Добавь описание",
                "Описание должно содержать от 5 до 2000 символов.",
                "Понятно");

            return;
        }

        var selectedInterest =
            InterestPicker.SelectedItem as InterestPickerOption;

        string? interestName = null;

        if (selectedInterest?.IsOther == true)
        {
            interestName = OtherInterestEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(interestName)
                || interestName.Length < 2)
            {
                await DisplayAlert(
                    "Укажи интерес",
                    "Напиши название интереса (не менее двух символов).",
                    "Понятно");

                return;
            }
        }
        else if (selectedInterest != null)
        {
            interestName = selectedInterest.Name;
        }

        var request = new CreateClubSuggestionRequest
        {
            Name = name,
            Description = description,

            // Передаём либо существующий интерес,
            // либо самостоятельно предложенный пользователем.
            InterestName = interestName,

            SuggestedAddress = string.IsNullOrWhiteSpace(AddressEntry.Text)
                ? null
                : AddressEntry.Text.Trim()
        };

        _isSubmitting = true;

        SubmitButton.IsEnabled = false;
        StatusLabel.Text = "Отправляем предложение...";

        try
        {
            await _apiService.PostAsync<
                CreateClubSuggestionRequest,
                ClubSuggestionDto>(
                    "api/ClubSuggestions",
                    request);

            await DisplayAlert(
                "Предложение отправлено",
                "Спасибо за идею! Её можно будет отслеживать в разделе «Мои предложения».",
                "Отлично");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось отправить предложение.";

            await DisplayAlert(
                "Ошибка",
                $"Попробуй ещё раз.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isSubmitting = false;
            SubmitButton.IsEnabled = true;
        }
    }
}
