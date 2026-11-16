
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using ClubPlatform.Mobile.Views.Clubs;

namespace ClubPlatform.Mobile.Views;

/// <summary>
/// Основная навигация мобильного приложения:
/// клубы, чаты и профиль.
/// </summary>
public sealed class MainTabsPage : TabbedPage
{
    public MainTabsPage(
        IApiService apiService,
        ITokenStorage tokenStorage)
    {
        Title = "Клубная платформа";

        // Цвета нижней панели.
        BarBackgroundColor = Color.FromArgb("#FFFFFF");
        BarTextColor = Color.FromArgb("#55402F");
        SelectedTabColor = Color.FromArgb("#8A6448");
        UnselectedTabColor = Color.FromArgb("#827263");

        // Вкладка 1: каталог клубов.
        var clubsPage = new ClubsPage(
            apiService,
            tokenStorage)
        {
            Title = "Клубы"
        };

        Children.Add(new NavigationPage(clubsPage)
        {
            Title = "Клубы"
        });

        // Вкладка 2: доступные переписки.
        var chatsPage = new ChatsPage(
            apiService,
            tokenStorage)
        {
            Title = "Чаты"
        };

        Children.Add(new NavigationPage(chatsPage)
        {
            Title = "Чаты"
        });

        // Вкладка 3: профиль пользователя.
        var profilePage = new ProfilePage(
            apiService,
            tokenStorage)
        {
            Title = "Профиль"
        };

        Children.Add(new NavigationPage(profilePage)
        {
            Title = "Профиль"
        });
    }
}
