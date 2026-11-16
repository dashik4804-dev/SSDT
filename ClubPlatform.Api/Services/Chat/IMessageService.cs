using ClubPlatform.Shared.DTOs.Messages;

namespace ClubPlatform.Api.Services.Chat;

/// <summary>
/// Работа с сообщениями чатов.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Возвращает историю сообщений клуба.
    /// </summary>
    Task<List<MessageDto>> GetHistoryAsync(
        Guid clubId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новое сообщение.
    /// </summary>
    Task<MessageDto> CreateAsync(
        Guid clubId,
        Guid userId,
        string content,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, может ли пользователь читать чат клуба.
    /// </summary>
    Task<bool> CanAccessChatAsync(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, может ли пользователь писать в клуб.
    /// </summary>
    Task<bool> CanSendMessageAsync(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken = default);
}