using ClubPlatform.Shared.DTOs.Interests;

namespace ClubPlatform.Api.Services.Interests;

/// <summary>
/// Сервис работы с интересами.
/// </summary>
public interface IInterestService
{
    /// <summary>
    /// Возвращает все интересы.
    /// </summary>
    Task<List<InterestDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает один интерес по ID.
    /// </summary>
    Task<InterestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новый интерес.
    /// </summary>
    Task<InterestDto> CreateAsync(
        CreateInterestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет интерес.
    /// </summary>
    Task<InterestDto?> UpdateAsync(
        Guid id,
        UpdateInterestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет интерес.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}