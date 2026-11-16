
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Shared.DTOs.Support;

namespace ClubPlatform.Mobile.Views.Support;

/// <summary>
/// Страница создания обращения в службу поддержки.
/// </summary>
public partial class CreateSupportTicketPage : ContentPage
{
    private readonly IApiService _apiService;

    private bool _isSending;

    public CreateSupportTicketPage(IApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        MessageEditor.TextChanged += (_, _) =>
        {
            CounterLabel.Text =
                $"{MessageEditor.Text?.Length ?? 0} / 4000";
        };
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (_isSending)
            return;

        var subject = SubjectEntry.Text?.Trim() ?? string.Empty;
        var message = MessageEditor.Text?.Trim() ?? string.Empty;

        if (subject.Length < 3)
        {
            await DisplayAlert(
                "Проверь тему",
                "Тема обращения должна содержать минимум 3 символа.",
                "Понятно");

            return;
        }

        if (message.Length < 10)
        {
            await DisplayAlert(
                "Проверь сообщение",
                "Сообщение должно содержать минимум 10 символов.",
                "Понятно");

            return;
        }

        _isSending = true;
        SendButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var request = new CreateSupportTicketRequest
            {
                Subject = subject,
                Message = message
            };

            var result =
                await _apiService.PostAsync<CreateSupportTicketRequest, SupportTicketDto>(
                    "api/support/tickets",
                    request,
                    CancellationToken.None);

            if (result == null)
            {
                await DisplayAlert(
                    "Ошибка",
                    "Не удалось отправить обращение.",
                    "Понятно");

                return;
            }

            await DisplayAlert(
                "Обращение отправлено",
                "Твоё обращение успешно отправлено администрации.",
                "Отлично");

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Ошибка отправки",
                $"Не удалось отправить обращение.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isSending = false;
            SendButton.IsEnabled = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}
