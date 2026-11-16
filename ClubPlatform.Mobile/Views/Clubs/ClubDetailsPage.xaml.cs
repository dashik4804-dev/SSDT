using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using System.Xml;

namespace ClubPlatform.Mobile.Views.Clubs;

public partial class ClubDetailsPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;

    // Клуб, который пользователь выбрал в каталоге.
    private ClubListItem _club;

    private bool _isAdmin;
    private bool _roleLoaded;

    private bool _isBusy;

    public ClubDetailsPage(
        IApiService apiService,
        ITokenStorage tokenStorage,
        ClubListItem club)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
        _club = club;

        DisplayClub(_club);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Получаем актуальную информацию с сервера.
        await LoadClubAsync();
    }

    /// <summary>
    /// Загружает подробные данные о выбранном клубе.
    /// </summary>
    private async Task LoadClubAsync()
    {
        try
        {
            var club = await _apiService.GetAsync<ClubListItem>(
                $"api/Clubs/{_club.Id}");

            if (club is not null)
            {
                // Один раз получаем роль текущего пользователя.
                if (!_roleLoaded)
                {
                    try
                    {
                        var currentUser =
                            await _apiService.GetAsync<UserProfileResponse>(
                                "api/Auth/me");

                        _isAdmin = string.Equals(
                            currentUser?.Role,
                            "Admin",
                            StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // Если профиль не удалось прочитать,
                        // не показываем административные возможности.
                        _isAdmin = false;
                    }

                    _roleLoaded = true;
                }

                // Обновляем данные клуба и кнопки.
                _club = club;
                DisplayClub(_club);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось загрузить клуб",
                $"Проверь подключение к серверу.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Отображает данные клуба на странице.
    /// </summary>
    private void DisplayClub(ClubListItem club)
    {
        NameLabel.Text = club.Name;

        DescriptionLabel.Text =
            string.IsNullOrWhiteSpace(club.Description)
                ? "Описание пока не добавлено."
                : club.Description;

        AddressLabel.Text =
            string.IsNullOrWhiteSpace(club.Address)
                ? "Адрес пока не указан."
                : $"Адрес: {club.Address}";

        MembersLabel.Text = club.MembersText;
        InterestsLabel.Text = club.InterestsText;

        // Не показываем пустые контактные поля.
        PhoneLabel.IsVisible =
            !string.IsNullOrWhiteSpace(club.ContactPhone);

        EmailLabel.IsVisible =
            !string.IsNullOrWhiteSpace(club.ContactEmail);

        PhoneLabel.Text =
            $"Телефон: {club.ContactPhone}";

        EmailLabel.Text =
            $"Email: {club.ContactEmail}";

        ContactLayout.IsVisible =
            PhoneLabel.IsVisible || EmailLabel.IsVisible;

        // Загружаем изображение по URL из базы данных.
        if (Uri.TryCreate(
            club.PhotoUrl,
            UriKind.Absolute,
            out var photoUri)
            && (photoUri.Scheme == Uri.UriSchemeHttp
                || photoUri.Scheme == Uri.UriSchemeHttps))
        {
            ClubPhoto.Source = ImageSource.FromUri(photoUri);
        }
        else
        {
            ClubPhoto.Source = null;
        }

        // Обычный пользователь может вступить, если ещё не состоит в клубе.
        JoinButton.IsVisible = !club.IsMember && !_isAdmin;
        JoinButton.IsEnabled = JoinButton.IsVisible;

        // Участник может выйти из клуба.
        LeaveButton.IsVisible = club.IsMember && !_isAdmin;
        LeaveButton.IsEnabled = LeaveButton.IsVisible;

        // Участники и администраторы могут открывать чат.
        ChatButton.IsVisible = club.IsMember || _isAdmin;

        // Просматривать участников клуба могут все пользователи.
        MembersButton.IsVisible = true;
    }

    /// <summary>
    /// Отправляет запрос на вступление в клуб.
    /// </summary>
    private async void OnJoinClicked(
        object sender,
        EventArgs e)
    {
        if (_isBusy || _club.IsMember)
            return;

        _isBusy = true;

        JoinButton.IsEnabled = false;
        JoinButton.Text = "Вступаем...";

        try
        {
            // Сервер определяет пользователя по JWT-токену.
            // Пустой объект передаётся, поскольку ID клуба уже есть в URL.
            await _apiService.PostAsync<object, object>(
                $"api/clubs/{_club.Id}/members",
                new { });

            // Если сервер подтвердил вступление, сразу обновляем экран.
            _club.IsMember = true;

            // Обновляем количество участников локально.
            if (!_club.MaxMembers.HasValue
                || _club.MaxMembers.Value <= 0
                || _club.MemberCount < _club.MaxMembers.Value)
            {
                _club.MemberCount++;
            }

            DisplayClub(_club);

            await DisplayAlert(
                "Готово!",
                $"Вы вступили в клуб «{_club.Name}».",
                "Отлично");
        }
        catch (Exception ex)
        {
            // Проверяем, запретил ли сервер вступление в клуб.
            if (ex.Message.Contains(
                "403",
                StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains(
                    "Администратор запретил",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Доступ к клубу ограничен",
                    "Администратор запретил тебе вступать в этот клуб. "
                    + "Если считаешь, что это ошибка, обратись к администрации.",
                    "Понятно");
            }
            else
            {
                // Для остальных ошибок показываем обычное сообщение.
                await DisplayAlert(
                    "Не удалось вступить",
                    "Сервер не разрешил вступление или произошла ошибка.\n\n"
                    + ex.Message,
                    "Понятно");
            }
        }
        finally
        {
            _isBusy = false;

            // Повторно активируем кнопку, если пользователь
            // так и не стал участником клуба.
            if (!_club.IsMember)
            {
                JoinButton.IsEnabled = true;
                JoinButton.Text = "?? Вступить в клуб";
            }
        }
    }

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadClubAsync();
    }

    /// <summary>
    /// Открывает чат выбранного клуба.
    /// </summary>
    private async void OnChatClicked(
        object sender,
        EventArgs e)
    {
        var chatPage = new ChatPage(
            _apiService,
            _tokenStorage,
            _club);

        await Navigation.PushAsync(chatPage);
    }

    // Открытие списка участников клуба доступно всем пользователям
    private async void OnMembersClicked(object sender, EventArgs e)
    {
        if (_club == null)
            return;

        await Navigation.PushAsync(
            new ClubMembersPage(_apiService, _club));
    }


    /// <summary>
    /// Позволяет участнику добровольно выйти из клуба.
    /// </summary>
    private async void OnLeaveClicked(
        object sender,
        EventArgs e)
    {
        if (_isBusy || !_club.IsMember || _isAdmin)
            return;

        bool confirmed = await DisplayAlert(
            "Выйти из клуба?",
            $"Вы действительно хотите выйти из клуба «{_club.Name}»?",
            "Выйти",
            "Отмена");

        if (!confirmed)
            return;

        _isBusy = true;
        LeaveButton.IsEnabled = false;

        try
        {
            // DELETE /api/clubs/{clubId}/members
            await _apiService.DeleteAsync(
                $"api/clubs/{_club.Id}/members");

            // Обновляем состояние страницы после ответа сервера.
            _club.IsMember = false;

            if (_club.MemberCount > 0)
            {
                _club.MemberCount--;
            }

            DisplayClub(_club);

            await DisplayAlert(
                "Готово",
                $"Вы вышли из клуба «{_club.Name}».",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось выйти",
                $"Сервер не смог выполнить выход из клуба.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isBusy = false;

            // Если сервер отклонил запрос, участник по-прежнему состоит в клубе.
            if (_club.IsMember && !_isAdmin)
            {
                LeaveButton.IsEnabled = true;
            }
        }
    }
}