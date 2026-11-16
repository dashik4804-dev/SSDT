using System.Collections.ObjectModel;
using System.Text.Json;
using ClubPlatform.Mobile.Constants;
using ClubPlatform.Mobile.Models;
using ClubPlatform.Mobile.Services.Api;
using ClubPlatform.Mobile.Services.Storage;
using Microsoft.AspNetCore.SignalR.Client;
using ClubPlatform.Shared.DTOs.Reports;
using ClubPlatform.Shared.DTOs.Messages;

namespace ClubPlatform.Mobile.Views.Clubs;

public partial class ChatPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;
    private readonly ClubListItem _club;

    private readonly ObservableCollection<ChatMessageItem> _messages = new();

    private HubConnection? _connection;
    private bool _isConnecting;
    private bool _isSending;
    private ChatMessageItem? _selectedMessage;
    private Border? _selectedBorder;
    // ID пользователя, который сейчас вошёл в приложение.
    private Guid _currentUserId;
    // Получено ли для текущего пользователя ограничение отправки.
    private bool _isMuted;
    // Показывает, что пользователь был исключён из клуба.
    private bool _wasRemovedFromClub;

    public ChatPage(
        IApiService apiService,
        ITokenStorage tokenStorage,
        ClubListItem club)
    {
        InitializeComponent();

        _apiService = apiService;
        _tokenStorage = tokenStorage;
        _club = club;

        ClubNameLabel.Text = $"Чат: {_club.Name}";
        MessagesCollection.ItemsSource = _messages;
        // До установления соединения отправка недоступна.
        MessageEntry.IsEnabled = false;
        SendButton.IsEnabled = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadHistoryAsync();
        await ConnectAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        var connection = _connection;
        _connection = null;

        if (connection is null)
            return;

        try
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await connection.InvokeAsync(
                    "LeaveClub",
                    _club.Id);
            }
        }
        catch
        {
            // При закрытии страницы соединение всё равно нужно завершить.
        }

        await connection.DisposeAsync();
    }

    /// <summary>
    /// Загружает ранее отправленные сообщения из API.
    /// </summary>
    private async Task LoadHistoryAsync()
    {
        try
        {
            var history =
                await _apiService.GetAsync<List<ChatMessageItem>>(
                    $"api/Messages/club/{_club.Id}");

            _messages.Clear();

            if (history is not null)
            {
                foreach (var message in history)
                {
                    PrepareMessage(message);
                    _messages.Add(message);
                }
            }

            ScrollToLastMessage(animate: false);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось загрузить историю";

            await DisplayAlert(
                "История сообщений",
                $"Не получилось получить сообщения.\n\n{ex.Message}",
                "Понятно");
        }
    }

    /// <summary>
    /// Создаёт SignalR-соединение и входит в группу чата клуба.
    /// </summary>
    private async Task ConnectAsync()
    {
        if (_isConnecting)
            return;

        if (_connection?.State == HubConnectionState.Connected)
            return;

        _isConnecting = true;
        StatusLabel.Text = "Подключаемся к чату...";

        try
        {
            // Определяем текущего пользователя по действующему JWT.
            var currentUser =
                await _apiService.GetAsync<UserProfileResponse>("api/Auth/me");

            if (currentUser is null || currentUser.Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Не удалось определить текущего пользователя.");
            }

            _currentUserId = currentUser.Id;

            // После определения пользователя обновляем
            // цвета и расположение загруженных сообщений.
            RefreshMessageAppearance();

            var hubUrl = new Uri(
                new Uri(ApiConstants.BaseUrl),
                "hubs/club-chat");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // Передаём JWT, сохранённый при входе.
                    options.AccessTokenProvider =
                        async () => await _tokenStorage.GetTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            _connection = connection;

            // При получении сообщения обновляем интерфейс на UI-потоке.
            connection.On<ChatMessageItem>(
                "ReceiveMessage",
                message =>
                {
                    MainThread.BeginInvokeOnMainThread(
                        () => AddMessage(message));
                });


            // Получаем уведомление от сервера,
            // если администратор исключил пользователя из клуба.
            connection.On<Guid>(
                "RemovedFromClub",
                removedClubId =>
                {
                    // Проверяем, что уведомление относится к открытому клубу.
                    if (removedClubId != _club.Id)
                        return;

                    MainThread.BeginInvokeOnMainThread(
                        async () => await HandleRemovedFromClubAsync());
                });


            // Получаем событие удаления сообщения от администратора.
            connection.On<JsonElement>(
                "MessageDeleted",
                payload =>
                {
                    if (!TryGetProperty(
                            payload,
                            "messageId",
                            "MessageId",
                            out var idValue)
                        || !Guid.TryParse(idValue.ToString(), out var messageId))
                    {
                        return;
                    }

                    MainThread.BeginInvokeOnMainThread(
                        () => HandleMessageDeleted(messageId));
                });

            // Получаем уведомление об изменении ограничения на отправку.
            connection.On<JsonElement>(
                "UserMuteChanged",
                payload =>
                {
                    if (!TryGetProperty(
                            payload,
                            "userId",
                            "UserId",
                            out var userValue)
                        || !Guid.TryParse(userValue.ToString(), out var userId))
                    {
                        return;
                    }

                    if (!TryGetProperty(
                            payload,
                            "isMuted",
                            "IsMuted",
                            out var mutedValue)
                        || (mutedValue.ValueKind != JsonValueKind.True
                            && mutedValue.ValueKind != JsonValueKind.False))
                    {
                        return;
                    }

                    var isMuted = mutedValue.GetBoolean();

                    MainThread.BeginInvokeOnMainThread(
                        () => HandleMuteChanged(userId, isMuted));
                });

            // После восстановления связи повторно входим в группу клуба.
            connection.Reconnected += async _ =>
            {
                try
                {
                    await connection.InvokeAsync(
                        "JoinClub",
                        _club.Id);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = "Чат подключён";
                        UpdateSendControls();
                    });
                }
                catch
                {
                    MainThread.BeginInvokeOnMainThread(
                        () => StatusLabel.Text =
                            "Не удалось повторно войти в чат");
                }
            };

            await connection.StartAsync();

            await connection.InvokeAsync(
                "JoinClub",
                _club.Id);

            StatusLabel.Text = "Чат подключён";
            UpdateSendControls();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Нет соединения с чатом";

            await DisplayAlert(
                "Не удалось подключиться",
                $"Проверь сервер и авторизацию.\n\n{ex.Message}",
                "Понятно");

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _isConnecting = false;
        }
    }


    /// <summary>
    /// Определяет, принадлежит ли сообщение
    /// текущему пользователю.
    /// </summary>
    private void PrepareMessage(ChatMessageItem message)
    {
        message.IsOwnMessage =
            _currentUserId != Guid.Empty
            && message.UserId == _currentUserId;
    }

    /// <summary>
    /// Обновляет оформление уже загруженной истории
    /// после определения текущего пользователя.
    /// </summary>
    private void RefreshMessageAppearance()
    {
        foreach (var message in _messages)
        {
            PrepareMessage(message);
        }

        // Пересоздаём элементы списка, чтобы обновились
        // цвета и расположение сообщений.
        MessagesCollection.ItemsSource = null;
        MessagesCollection.ItemsSource = _messages;

        // После повторной привязки CollectionView сбрасывает позицию.
        // Возвращаемся к последнему сообщению после обновления разметки.
        ScrollToLastMessage(animate: false);
    }



    /// <summary>
    /// Добавляет сообщение, полученное через SignalR.
    /// </summary>
    private void AddMessage(ChatMessageItem message)
    {
        // Не допускаем дублирования сообщений.
        if (message.EffectiveId != Guid.Empty
            && _messages.Any(
                item => item.EffectiveId == message.EffectiveId))
        {
            return;
        }

        // Определяем автора до добавления сообщения.
        PrepareMessage(message);

        _messages.Add(message);

        ScrollToLastMessage();
    }


    /// <summary>
    /// Помечает удалённое сообщение, не убирая его из истории.
    /// </summary>
    private void HandleMessageDeleted(Guid messageId)
    {
        var message = _messages.FirstOrDefault(
            item => item.EffectiveId == messageId);

        if (message is null || message.IsDeleted)
            return;

        // Свойство IsDeleted не вызывает автоматическое обновление UI,
        // поэтому после изменения обновляем источник элементов.
        message.IsDeleted = true;

        if (_selectedMessage?.EffectiveId == messageId)
        {
            ClearSelectedMessage();
        }

        MessagesCollection.ItemsSource = null;
        MessagesCollection.ItemsSource = _messages;
    }

    /// <summary>
    /// Обрабатывает изменение ограничения для текущего пользователя.
    /// </summary>
    private void HandleMuteChanged(Guid userId, bool isMuted)
    {
        // Событие рассылается всей группе клуба.
        // Меняем интерфейс только у того пользователя,
        // для которого администратор изменил ограничение.
        if (userId != _currentUserId)
            return;

        _isMuted = isMuted;

        UpdateSendControls();

        StatusLabel.Text = isMuted
            ? "Администратор запретил отправку сообщений в этом клубе."
            : "Ограничение снято. Можно отправлять сообщения.";
    }


    /// <summary>
    /// Обрабатывает принудительное исключение пользователя из клуба.
    /// </summary>
    private async Task HandleRemovedFromClubAsync()
    {
        if (_wasRemovedFromClub)
            return;

        _wasRemovedFromClub = true;

        // Блокируем отправку сообщений.
        MessageEntry.IsEnabled = false;
        SendButton.IsEnabled = false;

        StatusLabel.Text = "Ты больше не состоишь в этом клубе.";

        // Закрываем SignalR-соединение.
        var connection = _connection;
        _connection = null;

        if (connection is not null)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
                // Соединение уже могло быть закрыто сервером.
            }
        }

        await DisplayAlert(
            "Доступ к клубу ограничен",
            "Администратор удалил тебя из этого клуба. "
            + "Теперь ты не можешь читать и отправлять сообщения в его чате.",
            "Понятно");

        // Возвращаем пользователя к предыдущей странице.
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }


    /// <summary>
    /// Обновляет доступность поля ввода и кнопки отправки.
    /// </summary>
    private void UpdateSendControls()
    {
        var connected =
            _connection?.State == HubConnectionState.Connected;

        MessageEntry.IsEnabled = connected && !_isMuted;

        SendButton.IsEnabled =
            connected && !_isMuted && !_isSending;
    }

    /// <summary>
    /// Снимает выделение сообщения, выбранного для жалобы.
    /// </summary>
    private void ClearSelectedMessage()
    {
        if (_selectedBorder is not null)
        {
            _selectedBorder.Stroke =
                new SolidColorBrush(Color.FromArgb("#E8E2FA"));
        }

        _selectedBorder = null;
        _selectedMessage = null;

        SelectionLabel.Text =
            "Нажми на сообщение, чтобы выбрать его для жалобы";
    }

    /// <summary>
    /// Безопасно ищет JSON-поле по имени в camelCase или PascalCase.
    /// </summary>
    private static bool TryGetProperty(
        JsonElement payload,
        string camelCaseName,
        string pascalCaseName,
        out JsonElement value)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty(camelCaseName, out value))
                return true;

            if (payload.TryGetProperty(pascalCaseName, out value))
                return true;
        }

        value = default;
        return false;
    }

    private void ScrollToLastMessage(bool animate = true)
    {
        if (_messages.Count == 0)
            return;

        // Прокрутка должна выполняться после обновления размеров CollectionView.
        // На Android немедленный ScrollTo после ItemsSource часто не срабатывает.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
        {
            if (_messages.Count == 0 || !MessagesCollection.IsLoaded)
                return;

            MessagesCollection.ScrollTo(
                _messages[_messages.Count - 1],
                position: ScrollToPosition.End,
                animate: animate);
        });
    }

    private void OnSendClicked(
        object sender,
        EventArgs e)
    {
        _ = SendCurrentMessageAsync();
    }

    private void OnMessageCompleted(
        object sender,
        EventArgs e)
    {
        _ = SendCurrentMessageAsync();
    }

    /// <summary>
    /// Отправляет сообщение через SignalR.
    /// </summary>
    private async Task SendCurrentMessageAsync()
    {
        if (_isSending)
            return;

        var content = MessageEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return;

        if (_isMuted)
        {
            await DisplayAlert(
                "Отправка недоступна",
                "Администратор ограничил отправку сообщений в этом клубе.",
                "Понятно");

            return;
        }

        var connection = _connection;

        if (connection?.State != HubConnectionState.Connected)
        {
            await DisplayAlert(
                "Нет соединения",
                "Чат сейчас не подключён. Попробуй открыть его ещё раз.",
                "Понятно");

            return;
        }

        _isSending = true;
        SendButton.IsEnabled = false;

        try
        {
            await connection.InvokeAsync(
                "SendMessage",
                _club.Id,
                content);

            MessageEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось отправить",
                $"Сервер не принял сообщение.\n\n{ex.Message}",
                "Понятно");
        }
        finally
        {
            _isSending = false;
            UpdateSendControls();
        }
    }


    /// <summary>
    /// Позволяет участнику клуба пожаловаться на сообщение.
    /// </summary>
    private async void OnReportSelectedClicked(
        object sender,
        EventArgs e)
    {
        var message = _selectedMessage;

        if (message is null)
        {
            await DisplayAlert(
                "Выбери сообщение",
                "Сначала нажми на сообщение, на которое хочешь пожаловаться.",
                "Понятно");

            return;
        }

        // Нельзя отправить жалобу на уже удалённое сообщение.
        if (message.IsDeleted)
        {
            await DisplayAlert(
                "Жалоба",
                "Это сообщение уже удалено.",
                "Понятно");

            return;
        }

        // Проверяем, что у сообщения есть корректный идентификатор.
        var messageId = message.EffectiveId;

        if (messageId == Guid.Empty)
        {
            await DisplayAlert(
                "Ошибка",
                "Не удалось определить сообщение. Попробуй обновить чат.",
                "Понятно");

            return;
        }

        // Предлагаем пользователю выбрать причину жалобы.
        var reason = await DisplayActionSheet(
            "Почему Вы хотите пожаловаться?",
            "Отмена",
            null,
            "Оскорбления или травля",
            "Спам или реклама",
            "Неприемлемое содержание",
            "Другое");

        if (string.IsNullOrWhiteSpace(reason)
            || reason == "Отмена")
        {
            return;
        }

        // Для варианта «Другое» просим указать причину.
        if (reason == "Другое")
        {
            reason = await DisplayPromptAsync(
                "Причина жалобы",
                "Кратко опиши проблему:",
                accept: "Отправить",
                cancel: "Отмена",
                maxLength: 500,
                placeholder: "Описание нарушения");

            if (string.IsNullOrWhiteSpace(reason))
                return;

            reason = reason.Trim();
        }

        bool confirmed = await DisplayAlert(
            "Подтверждение",
            $"Отправить жалобу на сообщение пользователя "
            + $"«{message.DisplayAuthorName}»?",
            "Отправить",
            "Отмена");

        if (!confirmed)
            return;

        try
        {
            // Создаём жалобу через существующий endpoint API.
            var request = new CreateReportRequest
            {
                MessageId = messageId,
                Reason = reason
            };

            await _apiService.PostAsync<CreateReportRequest, ReportDto>(
                "api/Reports",
                request);

            // Снимаем выделение после успешной отправки жалобы.
            _selectedMessage = null;

            if (_selectedBorder is not null)
            {
                _selectedBorder.Stroke =
                    new SolidColorBrush(Color.FromArgb("#E8E2FA"));

                _selectedBorder = null;
            }

            SelectionLabel.Text =
                "Нажми на сообщение, чтобы выбрать его для жалобы";

            await DisplayAlert(
                "Жалоба отправлена",
                "Спасибо! Администратор сможет рассмотреть твою жалобу.",
                "Понятно");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Не удалось отправить жалобу",
                "Возможно, жалоба на это сообщение уже отправлена "
                + "или сервер отклонил запрос.\n\n"
                + ex.Message,
                "Понятно");
        }
    }

    /// <summary>
    /// Выбирает сообщение, на которое пользователь хочет пожаловаться.
    /// </summary>
    private async void OnMessageTapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border
            || border.BindingContext is not ChatMessageItem message)
        {
            return;
        }

        if (message.IsDeleted)
        {
            await DisplayAlert(
                "Жалоба",
                "На уже удалённое сообщение нельзя отправить жалобу.",
                "Понятно");

            return;
        }

        // Снимаем выделение с предыдущего сообщения.
        if (_selectedBorder is not null)
        {
            _selectedBorder.Stroke =
                new SolidColorBrush(Color.FromArgb("#E8E2FA"));
        }

        // Запоминаем выбранное сообщение и выделяем его рамку.
        _selectedMessage = message;
        _selectedBorder = border;

        // Выделяем сообщение фирменным фиолетовым цветом.
        _selectedBorder.Stroke =
            new SolidColorBrush(Color.FromArgb("#6235F5"));

        var content = message.DisplayContent;

        if (content.Length > 45)
        {
            content = content[..45] + "...";
        }

        SelectionLabel.Text =
            $"Выбрано: {message.DisplayAuthorName} — {content}";
    }

}
