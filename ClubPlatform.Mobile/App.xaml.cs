using ClubPlatform.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClubPlatform.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        // Сначала загружаем ресурсы приложения,
        // включая Colors.xaml и Styles.xaml.
        InitializeComponent();

        _serviceProvider = serviceProvider;

        // Показываем заставку.
        MainPage = new NavigationPage(new SplashPage());

        // После заставки открываем WelcomePage.
        MainPage.Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(2500);

            // Создаём WelcomePage только после того,
            // как ресурсы приложения уже загружены.
            var welcomePage =
                _serviceProvider.GetRequiredService<WelcomePage>();

            MainPage = new NavigationPage(welcomePage);
        });
    }
}
