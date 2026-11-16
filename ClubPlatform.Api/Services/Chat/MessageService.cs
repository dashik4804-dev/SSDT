using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.Messages;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Services.Chat;

/// <summary>
/// Реализация работы с сообщениями.
/// </summary>
public class MessageService : IMessageService
{
    private readonly AppDbContext _dbContext;

    public MessageService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Возвращает всю историю сообщений клуба.
    /// Новому участнику доступны старые сообщения.
    /// </summary>
    public async Task<List<MessageDto>> GetHistoryAsync(
        Guid clubId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ClubId == clubId)
            .OrderBy(message => message.CreatedAt)
            .Select(message => new MessageDto
            {
                Id = message.Id,

                ClubId = message.ClubId,

                UserId = message.UserId,

                UserName = message.User.Name,

                AuthorRole = message.User.Role,

                UserPhotoUrl = message.User.PhotoUrl,

                // Если сообщение удалено,
                // текст не возвращаем.
                Content = message.IsDeleted
                    ? "Сообщение удалено модератором."
                    : message.Content,

                CreatedAt = message.CreatedAt,

                IsDeleted = message.IsDeleted
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Создаёт новое сообщение.
    /// </summary>
    public async Task<MessageDto> CreateAsync(
        Guid clubId,
        Guid userId,
        string content,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        content = content.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Сообщение не может быть пустым.");
        }

        if (content.Length > 4000)
        {
            throw new ArgumentException(
                "Сообщение не может содержать более 4000 символов.");
        }

        // -----------------------------------------------------
        // Проверяем, что пользователь может писать.
        // -----------------------------------------------------

        ClubMember? member = null;

        // Обычный пользователь должен быть участником клуба.
        // Администратор может писать в чат без ClubMembers.
        if (!isAdmin)
        {
            member = await _dbContext.ClubMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.ClubId == clubId &&
                        item.UserId == userId,
                    cancellationToken);

            if (member is null)
            {
                throw new UnauthorizedAccessException(
                    "Пользователь не является участником клуба.");
            }
        }

        // -----------------------------------------------------
        // Проверяем mute.
        // -----------------------------------------------------

        // Mute применяется только к обычным участникам.
        // Администратор под mute не попадает.
        if (!isAdmin && member?.IsMuted == true)
        {
            throw new InvalidOperationException(
                "Пользователь не может отправлять сообщения: он находится в mute.");
        }

        // -----------------------------------------------------
        // Проверяем, что клуб существует.
        // -----------------------------------------------------

        var clubExists = await _dbContext.Clubs
            .AnyAsync(
                club => club.Id == clubId,
                cancellationToken);

        if (!clubExists)
        {
            throw new KeyNotFoundException(
                "Клуб не найден.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),

            ClubId = clubId,

            UserId = userId,

            Content = content,

            CreatedAt = DateTimeOffset.UtcNow,

            IsDeleted = false
        };

        _dbContext.Messages.Add(message);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        // Получаем данные автора.
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new
            {
                item.Name,
                item.PhotoUrl,
                item.Role
            })
            .FirstAsync(cancellationToken);

        return new MessageDto
        {
            Id = message.Id,

            ClubId = message.ClubId,

            UserId = message.UserId,

            UserName = user.Name,

            AuthorRole = user.Role,

            UserPhotoUrl = user.PhotoUrl,

            Content = message.Content,

            CreatedAt = message.CreatedAt,

            IsDeleted = false
        };
    }

    /// <summary>
    /// Проверяет, может ли пользователь читать чат клуба.
    /// Администратор проверяется отдельно в Hub.
    /// Здесь проверяем обычного участника.
    /// </summary>
    public async Task<bool> CanAccessChatAsync(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClubMembers
            .AnyAsync(
                member =>
                    member.ClubId == clubId &&
                    member.UserId == userId,
                cancellationToken);
    }

    /// <summary>
    /// Проверяет, может ли пользователь отправлять сообщение.
    /// </summary>
    public async Task<bool> CanSendMessageAsync(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClubMembers
            .AnyAsync(
                member =>
                    member.ClubId == clubId &&
                    member.UserId == userId &&
                    !member.IsMuted,
                cancellationToken);
    }
}