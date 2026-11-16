
using System;
using System.Collections.Generic;
using System.Linq;
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Support;

namespace ClubPlatform.Mobile.Views.Support;

/// <summary>
/// Страница просмотра и обработки обращений пользователей администратором.
/// </summary>
public partial class AdminSupportTicketsPage : ContentPage
{
    private readonly IApiService _apiService;

    public AdminSupportTicketsPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;
    }

    /// <summary>
    /// При открытии страницы загружаем обращения с сервера.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTicketsAsync();
    }

    /// <summary>
    /// Загружает все обращения пользователей.
    /// </summary>
    private async Task LoadTicketsAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var tickets = await _apiService.GetAsync<List<SupportTicketDto>>(
                "api/support/tickets");

            var items = tickets?
                .Select(ticket => new AdminSupportTicketItem
                {
                    Id = ticket.Id,
                    UserName = ticket.UserName,
                    Subject = ticket.Subject,
                    Message = ticket.Message,
                    Status = ticket.Status,
                    AdminResponse = ticket.AdminResponse,
                    CreatedAt = ticket.CreatedAt
                })
                .ToList();

            TicketsCollection.ItemsSource =
                items ?? new List<AdminSupportTicketItem>();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось загрузить обращения.\n{ex.Message}",
                "ОК");

            System.Diagnostics.Debug.WriteLine(
                $"Ошибка загрузки обращений: {ex}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    /// <summary>
    /// Отправляет ответ администратора на выбранное обращение.
    /// </summary>
    private async void OnSendResponseClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not AdminSupportTicketItem ticket)
        {
            return;
        }

        var response = ticket.ResponseDraft?.Trim();

        if (string.IsNullOrWhiteSpace(response))
        {
            await DisplayAlert(
                "Пустой ответ",
                "Напиши ответ пользователю перед отправкой.",
                "ОК");

            return;
        }

        bool confirmed = await DisplayAlert(
            "Отправить ответ?",
            $"Отправить ответ на обращение «{ticket.Subject}»?",
            "Отправить",
            "Отмена");

        if (!confirmed)
            return;

        button.IsEnabled = false;

        try
        {
            var request = new RespondSupportTicketRequest
            {
                Response = response
            };

            var result = await _apiService.PutAsync<
                RespondSupportTicketRequest,
                SupportTicketDto>(
                    $"api/support/tickets/{ticket.Id}/response",
                    request,
                    CancellationToken.None);

            if (result == null)
            {
                await DisplayAlert(
                    "Ошибка",
                    "Сервер не вернул результат отправки ответа.",
                    "ОК");

                return;
            }

            await DisplayAlert(
                "Готово",
                "Ответ успешно отправлен пользователю.",
                "ОК");

            // Обновляем список, чтобы показать новый статус и ответ.
            await LoadTicketsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось отправить ответ.\n{ex.Message}",
                "ОК");

            System.Diagnostics.Debug.WriteLine(
                $"Ошибка отправки ответа: {ex}");
        }
        finally
        {
            button.IsEnabled = true;
        }
    }
}
