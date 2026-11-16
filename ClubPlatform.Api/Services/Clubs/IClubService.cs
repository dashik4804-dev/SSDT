using ClubPlatform.Shared.DTOs.Clubs;

namespace ClubPlatform.Api.Services.Clubs;

/// <summary>
/// Сервис работы с клубами.
/// </summary>
public interface IClubService
{
    /// <summary>
    /// Возвращает список клубов.
    /// Если переданы интересы — используется условие OR:
    /// клуб подходит, если содержит хотя бы один выбранный интерес.
    /// </summary>
    Task<List<ClubDto>> GetAllAsync(
        List<Guid>? interestIds,
        Guid? currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает клуб по ID.
    /// </summary>
    Task<ClubDto?> GetByIdAsync(
        Guid id,
        Guid? currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт клуб.
    /// </summary>
    Task<ClubDto> CreateAsync(
        CreateClubRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет клуб.
    /// </summary>
    Task<ClubDto?> UpdateAsync(
        Guid id,
        UpdateClubRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет клуб.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}