using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.Clubs;
using ClubPlatform.Shared.DTOs.Interests;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Services.Clubs;

/// <summary>
/// Реализация бизнес-логики клубов.
/// </summary>
public class ClubService : IClubService
{
    private readonly AppDbContext _dbContext;

    public ClubService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получает список клубов.
    /// </summary>
    public async Task<List<ClubDto>> GetAllAsync(
        List<Guid>? interestIds,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Clubs
            .AsNoTracking()
            .AsQueryable();

        // -----------------------------------------------------
        // ФИЛЬТР ПО ИНТЕРЕСАМ
        // -----------------------------------------------------
        //
        // Например:
        // [Спорт, Книги, Музыка]
        //
        // Клуб попадёт в результат, если у него есть
        // хотя бы один из этих интересов.
        //
        // То есть:
        //
        // Спорт OR Книги OR Музыка
        //
        // Если список пустой — показываем все клубы.
        // -----------------------------------------------------

        if (interestIds is { Count: > 0 })
        {
            query = query.Where(club =>
                club.ClubInterests.Any(clubInterest =>
                    interestIds.Contains(clubInterest.InterestId)));
        }

        return await query
            .OrderBy(club => club.Name)
            .Select(club => new ClubDto
            {
                Id = club.Id,

                Name = club.Name,

                Description = club.Description,

                PhotoUrl = club.PhotoUrl,

                Address = club.Address,

                ContactPhone = club.ContactPhone,

                ContactEmail = club.ContactEmail,

                MaxMembers = club.MaxMembers,

                CreatedAt = club.CreatedAt,

                MembersCount = club.ClubMembers.Count,

                IsMember = currentUserId.HasValue &&
                           currentUserId.Value != Guid.Empty &&
                           club.ClubMembers.Any(member =>
                               member.UserId == currentUserId.Value),

                Interests = club.ClubInterests
                    .OrderBy(item => item.Interest.Name)
                    .Select(item => new InterestDto
                    {
                        Id = item.Interest.Id,
                        Name = item.Interest.Name,
                        Emoji = item.Interest.Emoji
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает конкретный клуб.
    /// </summary>
    public async Task<ClubDto?> GetByIdAsync(
        Guid id,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clubs
            .AsNoTracking()
            .Where(club => club.Id == id)
            .Select(club => new ClubDto
            {
                Id = club.Id,

                Name = club.Name,

                Description = club.Description,

                PhotoUrl = club.PhotoUrl,

                Address = club.Address,

                ContactPhone = club.ContactPhone,

                ContactEmail = club.ContactEmail,

                MaxMembers = club.MaxMembers,

                CreatedAt = club.CreatedAt,

                MembersCount = club.ClubMembers.Count,

                IsMember = currentUserId.HasValue &&
                           currentUserId.Value != Guid.Empty &&
                           club.ClubMembers.Any(member =>
                               member.UserId == currentUserId.Value),

                Interests = club.ClubInterests
                    .OrderBy(item => item.Interest.Name)
                    .Select(item => new InterestDto
                    {
                        Id = item.Interest.Id,
                        Name = item.Interest.Name,
                        Emoji = item.Interest.Emoji
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Создаёт новый клуб.
    /// </summary>
    public async Task<ClubDto> CreateAsync(
        CreateClubRequest request,
        CancellationToken cancellationToken = default)
    {
        var interestIds = request.InterestIds
            .Distinct()
            .ToList();

        // -----------------------------------------------------
        // Проверяем, что все переданные интересы существуют.
        // -----------------------------------------------------

        var existingInterestIds =
            await _dbContext.Interests
                .Where(interest =>
                    interestIds.Contains(interest.Id))
                .Select(interest => interest.Id)
                .ToListAsync(cancellationToken);

        if (existingInterestIds.Count != interestIds.Count)
        {
            throw new InvalidOperationException(
                "Один или несколько указанных интересов не существуют.");
        }

        // -----------------------------------------------------
        // Создаём клуб.
        // -----------------------------------------------------

        var club = new Club
        {
            Id = Guid.NewGuid(),

            Name = request.Name.Trim(),

            Description = request.Description.Trim(),

            PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? null
                : request.PhotoUrl.Trim(),

            Address = request.Address.Trim(),

            ContactPhone =
                string.IsNullOrWhiteSpace(request.ContactPhone)
                    ? null
                    : request.ContactPhone.Trim(),

            ContactEmail =
                string.IsNullOrWhiteSpace(request.ContactEmail)
                    ? null
                    : request.ContactEmail.Trim().ToLowerInvariant(),

            MaxMembers = request.MaxMembers,

            CreatedAt = DateTimeOffset.UtcNow
        };

        // -----------------------------------------------------
        // Добавляем интересы клуба.
        // -----------------------------------------------------

        foreach (var interestId in interestIds)
        {
            club.ClubInterests.Add(
                new ClubInterest
                {
                    ClubId = club.Id,
                    InterestId = interestId
                });
        }

        _dbContext.Clubs.Add(club);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        // Возвращаем уже созданный клуб.
        return await GetByIdAsync(
            club.Id,
            currentUserId: null,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Не удалось получить созданный клуб.");
    }

    /// <summary>
    /// Обновляет клуб.
    /// </summary>
    public async Task<ClubDto?> UpdateAsync(
        Guid id,
        UpdateClubRequest request,
        CancellationToken cancellationToken = default)
    {
        var club = await _dbContext.Clubs
            .Include(item => item.ClubInterests)
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (club is null)
        {
            return null;
        }

        var interestIds = request.InterestIds
            .Distinct()
            .ToList();

        // -----------------------------------------------------
        // Проверяем интересы.
        // -----------------------------------------------------

        var existingInterestIds =
            await _dbContext.Interests
                .Where(interest =>
                    interestIds.Contains(interest.Id))
                .Select(interest => interest.Id)
                .ToListAsync(cancellationToken);

        if (existingInterestIds.Count != interestIds.Count)
        {
            throw new InvalidOperationException(
                "Один или несколько указанных интересов не существуют.");
        }

        // -----------------------------------------------------
        // Обновляем основные поля клуба.
        // -----------------------------------------------------

        club.Name = request.Name.Trim();

        club.Description = request.Description.Trim();

        club.PhotoUrl =
            string.IsNullOrWhiteSpace(request.PhotoUrl)
                ? null
                : request.PhotoUrl.Trim();

        club.Address = request.Address.Trim();

        club.ContactPhone =
            string.IsNullOrWhiteSpace(request.ContactPhone)
                ? null
                : request.ContactPhone.Trim();

        club.ContactEmail =
            string.IsNullOrWhiteSpace(request.ContactEmail)
                ? null
                : request.ContactEmail.Trim().ToLowerInvariant();

        club.MaxMembers = request.MaxMembers;

        // -----------------------------------------------------
        // Обновляем связи с интересами.
        //
        // Для простоты заменяем полный набор связей.
        // -----------------------------------------------------

        _dbContext.ClubInterests.RemoveRange(
            club.ClubInterests);

        club.ClubInterests.Clear();

        foreach (var interestId in interestIds)
        {
            club.ClubInterests.Add(
                new ClubInterest
                {
                    ClubId = club.Id,
                    InterestId = interestId
                });
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return await GetByIdAsync(
            club.Id,
            currentUserId: null,
            cancellationToken);
    }

    /// <summary>
    /// Удаляет клуб.
    /// </summary>
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var club = await _dbContext.Clubs
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (club is null)
        {
            return false;
        }

        _dbContext.Clubs.Remove(club);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}