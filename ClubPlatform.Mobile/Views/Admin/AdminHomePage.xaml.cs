using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Views.Support;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class AdminHomePage : ContentPage
{
    private readonly IApiService _apiService;

    public AdminHomePage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    /// <summary>
    /// Открывает существующий экран жалоб.
    /// </summary>
    private async void OnReportsClicked(
        object sender,
        EventArgs e)
    {
        var page = new AdminPage(_apiService);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Открывает существующий экран управления клубами.
    /// </summary>
    private async void OnManageClubsClicked(
        object sender,
        EventArgs e)
    {
        var page = new ClubManagementPage(_apiService);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Открывает список пользователей с активными ограничениями.
    /// </summary>
    private async void OnMutedUsersClicked(
        object sender,
        EventArgs e)
    {
        var page = new MutedUsersPage(_apiService);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Открывает список предложений новых клубов.
    /// </summary>
    private async void OnClubSuggestionsClicked(
        object sender,
        EventArgs e)
    {
        var page = new AdminClubSuggestionsPage(_apiService);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Открывает управление интересами.
    /// </summary>
    private async void OnManageInterestsClicked(
        object sender,
        EventArgs e)
    {
        var page = new InterestManagementPage(_apiService);

        await Navigation.PushAsync(page);
    }

    /// <summary>
    /// Открывает обращения пользователей в службу поддержки.
    /// </summary>
    private async void OnSupportTicketsClicked(
        object sender,
        EventArgs e)
    {
        var page = new AdminSupportTicketsPage(_apiService);

        await Navigation.PushAsync(page);
    }
}
