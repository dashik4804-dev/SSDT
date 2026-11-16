using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Views.Users;
using ClubPlatform.Shared.DTOs.Auth;
using System;

namespace ClubPlatform.Mobile.Views.Clubs
{
    public partial class ClubMembersPage : ContentPage
    {
        private readonly IApiService _apiService;
        private readonly ClubListItem _club;
        private bool _isLoading;

        private bool _isAdmin;
        private bool _roleLoaded;

        public ClubMembersPage(IApiService apiService, ClubListItem club)
        {
            InitializeComponent();

            _apiService = apiService;
            _club = club;

            ClubNameLabel.Text = $"Участники: {_club.Name}";
        }

        // Открываем публичный профиль выбранного участника
        private async void OnMemberProfileTapped(
            object sender,
            TappedEventArgs e)
        {
            if (sender is not BindableObject element ||
                element.BindingContext is not ClubMemberItem member)
            {
                return;
            }

            await Navigation.PushAsync(
                new PublicUserProfilePage(
                    _apiService,
                    member.UserId));
        }

        // Загружаем участников при открытии страницы
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadMembersAsync();
        }

        // Получение списка участников выбранного клуба
        private async Task LoadMembersAsync()
        {
            if (_isLoading)
                return;

            // Проверяем роль текущего пользователя
            var currentUser =
                await _apiService.GetAsync<UserProfileResponse>("api/Auth/me");

            _isAdmin = string.Equals(
                currentUser?.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

            try
            {
                _isLoading = true;

                var members = await _apiService.GetAsync<List<ClubMemberItem>>(
                    $"api/clubs/{_club.Id}/members");

                // Настраиваем доступность удаления для каждого участника
                foreach (var member in members ?? new List<ClubMemberItem>())
                {
                    member.CanRemove = _isAdmin;
                }

                MembersCollection.ItemsSource = members ?? new List<ClubMemberItem>();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Ошибка",
                    $"Не удалось загрузить участников клуба.\n{ex.Message}",
                    "ОК");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // Обновление списка участников
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadMembersAsync();
        }

        // Удаление выбранного участника из клуба
        private async void OnRemoveMemberClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.BindingContext is not ClubMemberItem member)
            {
                return;
            }

            bool confirm = await DisplayAlert(
                "Удаление участника",
                $"Ты действительно хочешь удалить пользователя «{member.Name}» из клуба «{_club.Name}»?\n\nПосле удаления ему будет запрещено повторно вступать в этот клуб, пока администратор не снимет блокировку.",
                "Удалить",
                "Отмена");

            if (!confirm)
                return;

            try
            {
                await _apiService.DeleteAsync(
                    $"api/clubs/{_club.Id}/members/{member.UserId}");

                await DisplayAlert(
                    "Готово",
                    $"Пользователь «{member.Name}» удалён из клуба.",
                    "ОК");

                // Обновляем список после удаления
                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Ошибка удаления",
                    $"Не удалось удалить пользователя.\n{ex.Message}",
                    "ОК");
            }
        }
    }
}
