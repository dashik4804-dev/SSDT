
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Interests;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class InterestManagementPage : ContentPage
{
    private readonly IApiService _apiService;

    // ID интереса, который сейчас редактируется.
    // null означает создание нового интереса.
    private Guid? _editingId;

    private bool _isLoading;
    private bool _isSaving;

    public InterestManagementPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadInterestsAsync();
    }

    /// <summary>
    /// Загружает актуальный список интересов с сервера.
    /// </summary>
    private async Task LoadInterestsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем интересы...";

        try
        {
            var interests =
                await _apiService.GetAsync<List<InterestDto>>(
                    "api/Interests")
                ?? new List<InterestDto>();

            // Показываем интересы в алфавитном порядке.
            InterestsCollection.ItemsSource = interests
                .OrderBy(item => item.Name)
                .ToList();

            StatusLabel.Text = interests.Count switch
            {
                0 => "Интересов нет",
                1 => "Всего интересов: 1",
                _ => $"Всего интересов: {interests.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить интересы";

            await DisplayAlert(
                "Ошибка загрузки",
                $"Проверь подключение к серверу и права администратора.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Создаёт новый интерес или сохраняет изменения.
    /// </summary>
    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        if (_isSaving)
            return;

        var name = NameEntry.Text?.Trim();
        var emoji = string.IsNullOrWhiteSpace(EmojiEntry.Text)
            ? null
            : EmojiEntry.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            await DisplayAlert(
                "Проверь название",
                "Название интереса должно содержать от 2 до 100 символов.",
                "Понятно");

            return;
        }

        if (emoji is { Length: > 20 })
        {
            await DisplayAlert(
                "Проверь эмодзи",
                "Поле эмодзи не должно превышать 20 символов.",
                "Понятно");

            return;
        }

        _isSaving = true;
        SaveButton.IsEnabled = false;
        StatusLabel.Text = "Сохраняем интерес...";

        try
        {
            if (_editingId is null)
            {
                // Создание нового интереса.
                var request = new CreateInterestRequest
                {
                    Name = name,
                    Emoji = emoji
                };

                await _apiService.PostAsync<
                    CreateInterestRequest,
                    InterestDto>(
                        "api/Interests",
                        request);

                await DisplayAlert(
                    "Готово",
                    $"Интерес «{name}» добавлен.",
                    "Понятно");
            }
            else
            {
                // Изменение существующего интереса.
                var request = new UpdateInterestRequest
                {
                    Name = name,
                    Emoji = emoji
                };

                await _apiService.PutAsync<
                    UpdateInterestRequest,
                    InterestDto>(
                        $"api/Interests/{_editingId.Value}",
                        request);

                await DisplayAlert(
                    "Готово",
                    $"Интерес «{name}» обновлён.",
                    "Понятно");
            }

            // Возвращаем форму в режим создания.
            ClearForm();

            // Перезагружаем список из базы данных.
            await LoadInterestsAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось сохранить интерес";

            await DisplayAlert(
                "Ошибка сохранения",
                $"Возможно, интерес с таким названием уже существует.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isSaving = false;
            SaveButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Заполняет форму данными выбранного интереса.
    /// </summary>
    private void OnEditClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not InterestDto interest)
        {
            return;
        }

        _editingId = interest.Id;

        NameEntry.Text = interest.Name;
        EmojiEntry.Text = interest.Emoji;

        FormTitleLabel.Text = "Редактирование интереса";
        SaveButton.Text = "Сохранить изменения";
        CancelEditButton.IsVisible = true;

        StatusLabel.Text =
            $"Редактируется интерес «{interest.Name}».";
    }

    /// <summary>
    /// Отменяет редактирование и очищает форму.
    /// </summary>
    private void OnCancelEditClicked(
        object sender,
        EventArgs e)
    {
        ClearForm();
    }

    /// <summary>
    /// Удаляет выбранный интерес после подтверждения.
    /// </summary>
    private async void OnDeleteClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not InterestDto interest)
        {
            return;
        }

        bool confirmed = await DisplayAlert(
            "Удаление интереса",
            $"Удалить интерес «{interest.Name}»?\n\n"
            + "Он также может перестать отображаться у клубов, "
            + "к которым был привязан.",
            "Удалить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            await _apiService.DeleteAsync(
                $"api/Interests/{interest.Id}");

            // Если удалён интерес из редактируемой формы,
            // очищаем форму.
            if (_editingId == interest.Id)
            {
                ClearForm();
            }

            await LoadInterestsAsync();

            await DisplayAlert(
                "Готово",
                $"Интерес «{interest.Name}» удалён.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось удалить интерес",
                $"Сервер отклонил удаление или произошла ошибка.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Очищает поля и возвращает форму в режим создания.
    /// </summary>
    private void ClearForm()
    {
        _editingId = null;

        NameEntry.Text = string.Empty;
        EmojiEntry.Text = string.Empty;

        FormTitleLabel.Text = "Новый интерес";
        SaveButton.Text = "Добавить интерес";
        CancelEditButton.IsVisible = false;

        StatusLabel.Text = "Готово к работе.";
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadInterestsAsync();
    }
}
