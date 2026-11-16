using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.Interests;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Services.Interests;

/// <summary>
/// Реализация сервиса интересов.
/// </summary>
public class InterestService : IInterestService
{
    private readonly AppDbContext _dbContext;

    public InterestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получает все интересы.
    /// </summary>
    public async Task<List<InterestDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Interests
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new InterestDto
            {
                Id = x.Id,
                Name = x.Name,
                Emoji = x.Emoji
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает интерес по ID.
    /// </summary>
    public async Task<InterestDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Interests
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new InterestDto
            {
                Id = x.Id,
                Name = x.Name,
                Emoji = x.Emoji
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Создаёт новый интерес.
    /// </summary>
    public async Task<InterestDto> CreateAsync(
        CreateInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        // Проверяем уникальность имени.
        var exists = await _dbContext.Interests
            .AnyAsync(
                x => x.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "Интерес с таким названием уже существует.");
        }

        var interest = new Interest
        {
            Id = Guid.NewGuid(),
            Name = name,
            Emoji = string.IsNullOrWhiteSpace(request.Emoji)
                ? null
                : request.Emoji.Trim()
        };

        _dbContext.Interests.Add(interest);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new InterestDto
        {
            Id = interest.Id,
            Name = interest.Name,
            Emoji = interest.Emoji
        };
    }

    /// <summary>
    /// Обновляет существующий интерес.
    /// </summary>
    public async Task<InterestDto?> UpdateAsync(
        Guid id,
        UpdateInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var interest = await _dbContext.Interests
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (interest is null)
        {
            return null;
        }

        var name = request.Name.Trim();

        // Проверяем, не занято ли новое имя
        // другим интересом.
        var duplicateExists = await _dbContext.Interests
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.Name.ToLower() == name.ToLower(),
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Интерес с таким названием уже существует.");
        }

        interest.Name = name;

        interest.Emoji =
            string.IsNullOrWhiteSpace(request.Emoji)
                ? null
                : request.Emoji.Trim();

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new InterestDto
        {
            Id = interest.Id,
            Name = interest.Name,
            Emoji = interest.Emoji
        };
    }

    /// <summary>
    /// Удаляет интерес.
    /// </summary>
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var interest = await _dbContext.Interests
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (interest is null)
        {
            return false;
        }

        _dbContext.Interests.Remove(interest);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }
}