using ClubPlatform.Shared.DTOs.Reports;
using ClubPlatform.Shared.Enums;

namespace ClubPlatform.Api.Services.Moderation;

/// <summary>
/// Сервис модерации сообщений и участников.
/// </summary>
public interface IModerationService
{
    /// <summary>
    /// Получает список жалоб.
    /// </summary>
    Task<List<ReportDto>> GetReportsAsync(
        ReportStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает жалобы, отправленные конкретным пользователем.
    /// </summary>
    Task<List<ReportDto>> GetMyReportsAsync(
        Guid reporterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт жалобу на сообщение.
    /// </summary>
    Task<ReportDto> CreateReportAsync(
        Guid messageId,
        Guid reporterUserId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Изменяет статус жалобы.
    /// </summary>
    Task<ReportDto?> UpdateReportStatusAsync(
        Guid reportId,
        ReportStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Мягко удаляет сообщение.
    /// </summary>
    Task<bool> DeleteMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Устанавливает или снимает Mute
    /// с пользователя в конкретном клубе.
    /// </summary>
    Task<bool> SetMuteAsync(
        Guid clubId,
        Guid userId,
        bool isMuted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает всех участников, которым запрещено писать в клубах.
    /// </summary>
    Task<List<MutedClubMemberDto>> GetMutedMembersAsync(
        CancellationToken cancellationToken = default);
}