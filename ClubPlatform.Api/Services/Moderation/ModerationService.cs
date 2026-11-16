using ClubPlatform.Api.Data;
using ClubPlatform.Api.Hubs;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.Reports;
using ClubPlatform.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Services.Moderation;

/// <summary>
/// Реализация модерации сообщений и участников.
/// </summary>
public class ModerationService : IModerationService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<ClubChatHub> _hubContext;

    public ModerationService(
        AppDbContext dbContext,
        IHubContext<ClubChatHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    // =========================================================
    // ПОЛУЧЕНИЕ ЖАЛОБ
    // =========================================================

    /// <summary>
    /// Возвращает список жалоб.
    /// </summary>
    public async Task<List<ReportDto>> GetReportsAsync(
        ReportStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Reports
            .AsNoTracking()
            .AsQueryable();

        // Если статус указан — фильтруем по нему.
        if (status.HasValue)
        {
            query = query.Where(
                report => report.Status == status.Value);
        }

        return await query
            .OrderByDescending(report => report.CreatedAt)
            .Select(report => new ReportDto
            {
                Id = report.Id,

                MessageId = report.MessageId,

                ClubId = report.Message.ClubId,

                ClubName = report.Message.Club.Name,

                MessageAuthorUserId =
                    report.Message.UserId,

                MessageAuthorName =
                    report.Message.User.Name,

                MessageAuthorRole =
                    report.Message.User.Role,

                MessageContent =
                    report.Message.IsDeleted
                        ? "Сообщение удалено модератором."
                        : report.Message.Content,

                MessageCreatedAt =
                    report.Message.CreatedAt,

                IsMessageDeleted =
                    report.Message.IsDeleted,

                ReporterUserId =
                    report.ReporterUserId,

                ReporterName =
                    report.ReporterUser.Name,

                Reason = report.Reason,

                Status = report.Status,

                CreatedAt = report.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }


    /// <summary>
    /// Получает историю жалоб конкретного пользователя.
    /// </summary>
    public async Task<List<ReportDto>> GetMyReportsAsync(
        Guid reporterUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reports
            .AsNoTracking()
            .Where(report => report.ReporterUserId == reporterUserId)
            .OrderByDescending(report => report.CreatedAt)
            .Select(report => new ReportDto
            {
                Id = report.Id,
                MessageId = report.MessageId,
                ClubId = report.Message.ClubId,
                ClubName = report.Message.Club.Name,

                MessageAuthorUserId = report.Message.UserId,
                MessageAuthorName = report.Message.User.Name,
                MessageAuthorRole = report.Message.User.Role,

                MessageContent = report.Message.IsDeleted
                    ? "Сообщение удалено модератором."
                    : report.Message.Content,

                MessageCreatedAt = report.Message.CreatedAt,
                IsMessageDeleted = report.Message.IsDeleted,

                ReporterUserId = report.ReporterUserId,
                ReporterName = report.ReporterUser.Name,

                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }


    /// <summary>
    /// Получает список всех активных ограничений на отправку сообщений.
    /// </summary>
    public async Task<List<MutedClubMemberDto>> GetMutedMembersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClubMembers
            .AsNoTracking()
            .Where(member => member.IsMuted)
            .OrderBy(member => member.Club.Name)
            .ThenBy(member => member.User.Name)
            .Select(member => new MutedClubMemberDto
            {
                ClubId = member.ClubId,
                ClubName = member.Club.Name,
                UserId = member.UserId,
                UserName = member.User.Name,
                JoinedAt = member.JoinedAt
            })
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // СОЗДАНИЕ ЖАЛОБЫ
    // =========================================================

    /// <summary>
    /// Создаёт жалобу пользователя на сообщение.
    /// </summary>
    public async Task<ReportDto> CreateReportAsync(
        Guid messageId,
        Guid reporterUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        reason = reason.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Причина жалобы не может быть пустой.");
        }

        if (reason.Length > 1000)
        {
            throw new ArgumentException(
                "Причина жалобы не может превышать 1000 символов.");
        }

        // -----------------------------------------------------
        // Получаем сообщение.
        // -----------------------------------------------------

        var message = await _dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == messageId,
                cancellationToken);

        if (message is null)
        {
            throw new KeyNotFoundException(
                "Сообщение не найдено.");
        }

        if (message.IsDeleted)
        {
            throw new InvalidOperationException(
                "Нельзя пожаловаться на уже удалённое сообщение.");
        }

        // -----------------------------------------------------
        // Пользователь должен быть участником этого клуба.
        // -----------------------------------------------------

        var isMember = await _dbContext.ClubMembers
            .AnyAsync(
                member =>
                    member.ClubId == message.ClubId &&
                    member.UserId == reporterUserId,
                cancellationToken);

        if (!isMember)
        {
            throw new UnauthorizedAccessException(
                "Только участник клуба может пожаловаться на сообщение.");
        }

        // -----------------------------------------------------
        // Проверяем повторную жалобу.
        // -----------------------------------------------------

        var alreadyReported =
            await _dbContext.Reports
                .AnyAsync(
                    report =>
                        report.MessageId == messageId &&
                        report.ReporterUserId == reporterUserId,
                    cancellationToken);

        if (alreadyReported)
        {
            throw new InvalidOperationException(
                "Вы уже пожаловались на это сообщение.");
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),

            MessageId = messageId,

            ReporterUserId = reporterUserId,

            Reason = reason,

            Status = ReportStatus.Pending,

            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Reports.Add(report);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return await GetReportByIdAsync(
            report.Id,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Не удалось получить созданную жалобу.");
    }

    // =========================================================
    // ИЗМЕНЕНИЕ СТАТУСА
    // =========================================================

    /// <summary>
    /// Изменяет статус жалобы.
    /// </summary>
    /// <summary>
    /// Отклоняет новую жалобу.
    /// Повторная обработка закрытых жалоб запрещена.
    /// </summary>
    public async Task<ReportDto?> UpdateReportStatusAsync(
        Guid reportId,
        ReportStatus status,
        CancellationToken cancellationToken = default)
    {
        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(
                item => item.Id == reportId,
                cancellationToken);

        if (report is null)
        {
            return null;
        }

        // Через этот метод разрешено только отклонять жалобы.
        if (status != ReportStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Допускается только отклонение жалобы.");
        }

        // Обрабатывать можно только новые жалобы.
        if (report.Status != ReportStatus.Pending)
        {
            throw new InvalidOperationException(
                "Эта жалоба уже была обработана.");
        }

        report.Status = ReportStatus.Rejected;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return await GetReportByIdAsync(
            report.Id,
            cancellationToken);
    }

    // =========================================================
    // УДАЛЕНИЕ СООБЩЕНИЯ
    // =========================================================

    /// <summary>
    /// Soft delete сообщения.
    /// </summary>
    public async Task<bool> DeleteMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(
                item => item.Id == messageId,
                cancellationToken);

        if (message is null)
        {
            return false;
        }

        // Если уже удалено — считаем операцию успешной.
        if (message.IsDeleted)
        {
            return true;
        }

        message.IsDeleted = true;

        // Все ожидающие жалобы на удалённое сообщение
        // считаем обработанными.
        var pendingReports =
            await _dbContext.Reports
                .Where(
                    report =>
                        report.MessageId == messageId &&
                        report.Status == ReportStatus.Pending)
                .ToListAsync(cancellationToken);

        foreach (var report in pendingReports)
        {
            report.Status = ReportStatus.Resolved;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        // Уведомляем всех подключённых участников клуба,
        // чтобы приложение сразу убрало текст сообщения.
        var groupName = $"club:{message.ClubId}";

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync(
                "MessageDeleted",
                new
                {
                    MessageId = message.Id
                },
                cancellationToken);

        return true;
    }

    // =========================================================
    // MUTE
    // =========================================================

    /// <summary>
    /// Устанавливает или снимает Mute.
    /// </summary>
    public async Task<bool> SetMuteAsync(
        Guid clubId,
        Guid userId,
        bool isMuted,
        CancellationToken cancellationToken = default)
    {
        var member = await _dbContext.ClubMembers
            .Include(item => item.User)
            .FirstOrDefaultAsync(
                item =>
                    item.ClubId == clubId &&
                    item.UserId == userId,
                cancellationToken);

        if (member is null)
        {
            return false;
        }

        // Администратор не должен становиться
        // объектом обычной клубной модерации.
        if (member.User.Role == UserRole.Admin)
        {
            throw new InvalidOperationException(
                "Нельзя установить Mute администратору.");
        }

        // Если значение уже такое — ничего менять не нужно.
        if (member.IsMuted == isMuted)
        {
            return true;
        }

        member.IsMuted = isMuted;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        // Уведомляем всех клиентов клуба.
        var groupName = $"club:{clubId}";

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync(
                "UserMuteChanged",
                new
                {
                    UserId = userId,
                    IsMuted = isMuted
                },
                cancellationToken);

        return true;
    }

    // =========================================================
    // ВСПОМОГАТЕЛЬНЫЙ МЕТОД
    // =========================================================

    /// <summary>
    /// Получает жалобу по ID.
    /// </summary>
    private async Task<ReportDto?> GetReportByIdAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Reports
            .AsNoTracking()
            .Where(report => report.Id == reportId)
            .Select(report => new ReportDto
            {
                Id = report.Id,

                MessageId = report.MessageId,

                ClubId = report.Message.ClubId,

                ClubName = report.Message.Club.Name,

                MessageAuthorUserId =
                    report.Message.UserId,

                MessageAuthorName =
                    report.Message.User.Name,

                MessageAuthorRole =
                    report.Message.User.Role,

                MessageContent =
                    report.Message.IsDeleted
                        ? "Сообщение удалено модератором."
                        : report.Message.Content,

                MessageCreatedAt =
                    report.Message.CreatedAt,

                IsMessageDeleted =
                    report.Message.IsDeleted,

                ReporterUserId =
                    report.ReporterUserId,

                ReporterName =
                    report.ReporterUser.Name,

                Reason = report.Reason,

                Status = report.Status,

                CreatedAt = report.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}