
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Support;

namespace ClubPlatform.Mobile.Views.Support;

/// <summary>
/// Страница просмотра обращений текущего пользователя.
/// </summary>
public partial class MySupportTicketsPage : ContentPage
{
    private readonly IApiService _apiService;

    private bool _isLoading;

    public MySupportTicketsPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTicketsAsync();
    }

    /// <summary>
    /// Загружает обращения текущего пользователя с сервера.
    /// </summary>
    private async Task LoadTicketsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var tickets =
                await _apiService.GetAsync<List<SupportTicketDto>>(
                    "api/support/tickets/my");

            var items = tickets?
                .Select(ticket => new SupportTicketViewModel
                {
                    Subject = ticket.Subject,
                    Message = ticket.Message,
                    AdminResponse = ticket.AdminResponse,
                    Status = ticket.Status,
                    CreatedAt = ticket.CreatedAt,
                    AnsweredAt = ticket.AnsweredAt
                })
                .ToList() ?? new List<SupportTicketViewModel>();

            TicketsCollection.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось загрузить обращения.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            _isLoading = false;
        }
    }
}
