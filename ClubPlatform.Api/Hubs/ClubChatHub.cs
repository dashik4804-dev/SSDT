
using System.Security.Claims;
using ClubPlatform.Api.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClubPlatform.Api.Hubs;

/// <summary>
/// SignalR Hub для чатов клубов.
/// </summary>
[Authorize]
public class ClubChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly ClubConnectionTracker _connectionTracker;

    public ClubChatHub(
        IMessageService messageService,
        ClubConnectionTracker connectionTracker)
    {
        _messageService = messageService;
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Пользователь входит в SignalR-группу клуба.
    /// </summary>
    public async Task JoinClub(Guid clubId)
    {
        var userId = GetCurrentUserId();

        // Администратор может подключаться к чату
        // для модерации, даже не являясь участником.
        var isAdmin = Context.User?.IsInRole("Admin") == true;

        if (!isAdmin)
        {
            var canAccess = await _messageService.CanAccessChatAsync(
                clubId,
                userId,
                Context.ConnectionAborted);

            if (!canAccess)
            {
                throw new HubException(
                    "Вы не являетесь участником клуба.");
            }
        }

        var groupName = GetGroupName(clubId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        // Запоминаем подключение обычного пользователя.
        if (!isAdmin)
        {
            _connectionTracker.Add(
                clubId,
                userId,
                Context.ConnectionId);
        }
    }

    /// <summary>
    /// Пользователь покидает SignalR-группу клуба.
    /// </summary>
    public async Task LeaveClub(Guid clubId)
    {
        var userId = GetCurrentUserId();
        var groupName = GetGroupName(clubId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        _connectionTracker.Remove(
            clubId,
            userId,
            Context.ConnectionId);
    }

    /// <summary>
    /// Отправляет сообщение в чат.
    /// </summary>
    public async Task SendMessage(
        Guid clubId,
        string content)
    {
        var userId = GetCurrentUserId();

        var isAdmin = Context.User?.IsInRole("Admin") == true;

        try
        {
            var message = await _messageService.CreateAsync(
                clubId,
                userId,
                content,
                isAdmin,
                Context.ConnectionAborted);

            var groupName = GetGroupName(clubId);

            await Clients.Group(groupName)
                .SendAsync(
                    "ReceiveMessage",
                    message,
                    Context.ConnectionAborted);
        }
        catch (ArgumentException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    /// <summary>
    /// Убирает отключившееся соединение из отслеживания.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.RemoveConnectionFromAllClubs(
            Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Получает ID текущего пользователя из JWT.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new HubException(
                "Не удалось определить пользователя.");
        }

        return userId;
    }

    /// <summary>
    /// Формирует имя SignalR-группы клуба.
    /// </summary>
    private static string GetGroupName(Guid clubId)
    {
        return $"club:{clubId}";
    }
}
