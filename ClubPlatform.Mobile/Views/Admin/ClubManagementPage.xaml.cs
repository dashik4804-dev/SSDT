using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class ClubManagementPage : ContentPage
{
    private readonly IApiService _apiService;
    private bool _isLoading;

    public ClubManagementPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadClubsAsync();
    }

    /// <summary>
    /// Загружает актуальный список клубов с сервера.
    /// </summary>
    private async Task LoadClubsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем клубы...";

        try
        {
            var clubs =
                await _apiService.GetAsync<List<ClubListItem>>(
                    "api/Clubs")
                ?? new List<ClubListItem>();

            ClubsCollection.ItemsSource = clubs;

            StatusLabel.Text = clubs.Count switch
            {
                0 => "Клубов нет",
                1 => "Всего клубов: 1",
                _ => $"Всего клубов: {clubs.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить клубы";

            await DisplayAlert(
                "Ошибка",
                $"Проверь подключение к серверу и авторизацию.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnCreateClicked(
        object sender,
        EventArgs e)
    {
        var editor = new ClubEditorPage(_apiService);
        await Navigation.PushAsync(editor);
    }

    private async void OnEditClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not ClubListItem club)
        {
            return;
        }

        var editor = new ClubEditorPage(_apiService, club);
        await Navigation.PushAsync(editor);
    }

    private async void OnDeleteClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not BindableObject element
            || element.BindingContext is not ClubListItem club)
        {
            return;
        }

        bool confirmed = await DisplayAlert(
            "Удаление клуба",
            $"Вы действительно хотите удалить клуб «{club.Name}»?\n\n"
            + "Это действие может удалить связанные с ним данные.",
            "Удалить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            await _apiService.DeleteAsync(
                $"api/Clubs/{club.Id}");

            await LoadClubsAsync();

            await DisplayAlert(
                "Готово",
                "Клуб удалён.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось удалить клуб",
                $"Сервер отклонил удаление или произошла ошибка.\n\n{ex.Message}",
                "Понятно");
        }
    }
}