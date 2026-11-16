
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Reports;
using ClubPlatform.Shared.Enums;
using System.Text.Json;

namespace ClubPlatform.Mobile.Views.Admin;

public partial class AdminPage : ContentPage
{
    private readonly IApiService _apiService;
    private bool _isLoading;

    public AdminPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadReportsAsync();
    }

    /// <summary>
    /// Загружает список жалоб с сервера.
    /// </summary>
    private async Task LoadReportsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        StatusLabel.Text = "Загружаем жалобы...";

        try
        {
            var reports =
                await _apiService.GetAsync<List<ReportDto>>(
                    "api/Reports")
                ?? new List<ReportDto>();

            // Сначала показываем нерассмотренные жалобы,
            // внутри каждой группы — новые сверху.
            var orderedReports = reports
                .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            ReportsCollection.ItemsSource = orderedReports;

            StatusLabel.Text = orderedReports.Count switch
            {
                0 => "Жалоб нет",
                1 => "Всего жалоб: 1",
                _ => $"Всего жалоб: {orderedReports.Count}"
            };
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить жалобы";

            await DisplayAlert(
                "Ошибка",
                $"Проверь авторизацию и подключение к серверу.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Получает жалобу, к карточке которой относится кнопка.
    /// </summary>
    private static ReportDto? GetReport(object sender)
    {
        if (sender is BindableObject element
            && element.BindingContext is ReportDto report)
        {
            return report;
        }

        return null;
    }

    /// <summary>
    /// Отклоняет жалобу.
    /// </summary>
    private async void OnRejectClicked(
        object sender,
        EventArgs e)
    {
        await ChangeStatusAsync(sender, ReportStatus.Rejected);
    }

    /// <summary>
    /// Изменяет статус жалобы.
    /// </summary>
    private async Task ChangeStatusAsync(
        object sender,
        ReportStatus newStatus)
    {
        var report = GetReport(sender);

        // Действия доступны только для новых жалоб.
        if (report is null || report.Status != ReportStatus.Pending)
            return;

        var statusText = "отклонить";

        bool confirmed = await DisplayAlert(
            "Подтверждение",
            $"Вы действительно хотите {statusText} жалобу на сообщение пользователя "
            + $"«{report.MessageAuthorName}»?",
            "Подтвердить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            var request = new UpdateReportStatusRequest
            {
                Status = newStatus
            };

            await _apiService.PutAsync<
                UpdateReportStatusRequest,
                JsonElement>(
                    $"api/Reports/{report.Id}/status",
                    request);

            await LoadReportsAsync();

            await DisplayAlert(
                "Готово",
                "Статус жалобы обновлён.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось изменить статус жалобы.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Удаляет сообщение после подтверждения администратора.
    /// </summary>
    private async void OnDeleteMessageClicked(
        object sender,
        EventArgs e)
    {
        var report = GetReport(sender);

        // Удалять сообщение можно только по новой, ещё не обработанной жалобе.
        if (report is null
            || report.Status != ReportStatus.Pending
            || report.IsMessageDeleted)
        {
            return;
        }

        bool confirmed = await DisplayAlert(
            "Удаление сообщения",
            $"Удалить сообщение пользователя «{report.MessageAuthorName}» "
            + $"в клубе «{report.ClubName}»?",
            "Удалить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            await _apiService.DeleteAsync(
                $"api/Reports/messages/{report.MessageId}");

            await LoadReportsAsync();

            await DisplayAlert(
                "Готово",
                "Сообщение удалено.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка",
                $"Не удалось удалить сообщение.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Обновляет список жалоб.
    /// </summary>
    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        await LoadReportsAsync();
    }
}
