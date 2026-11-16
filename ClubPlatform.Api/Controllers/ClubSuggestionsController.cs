
using System.Security.Claims;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.ClubSuggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Предложения пользователей о создании новых клубов.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClubSuggestionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ClubSuggestionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Создаёт предложение от текущего пользователя.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClubSuggestionDto>> Create(
        [FromBody] CreateClubSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var suggestion = new ClubSuggestion
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),

            InterestName = string.IsNullOrWhiteSpace(request.InterestName)
                ? null
                : request.InterestName.Trim(),

            // Сохраняем адрес, если пользователь его указал.
            SuggestedAddress = string.IsNullOrWhiteSpace(
                request.SuggestedAddress)
                    ? null
                    : request.SuggestedAddress.Trim(),

            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ClubSuggestions.Add(suggestion);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.ClubSuggestions
            .AsNoTracking()
            .Include(x => x.User)
            .FirstAsync(
                x => x.Id == suggestion.Id,
                cancellationToken);

        return Ok(ToDto(saved));
    }

    /// <summary>
    /// Получает предложения текущего пользователя.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<ClubSuggestionDto>>> GetMine(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var suggestions = await _dbContext.ClubSuggestions
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(suggestions.Select(ToDto).ToList());
    }

    /// <summary>
    /// Получает все предложения. Доступно только администратору.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ClubSuggestionDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var suggestions = await _dbContext.ClubSuggestions
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(suggestions.Select(ToDto).ToList());
    }

    /// <summary>
    /// Принимает или отклоняет предложение клуба.
    /// </summary>
    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubSuggestionDto>> Review(
        Guid id,
        [FromBody] ReviewClubSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status.Trim();

        // Разрешаем только два решения администратора.
        if (!string.Equals(
                status, "Approved",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                status, "Rejected",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Статус должен быть Approved или Rejected."
            });
        }

        var suggestion = await _dbContext.ClubSuggestions
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (suggestion is null)
        {
            return NotFound(new
            {
                message = "Предложение не найдено."
            });
        }

        suggestion.Status =
            string.Equals(
                status,
                "Approved",
                StringComparison.OrdinalIgnoreCase)
                ? "Approved"
                : "Rejected";

        suggestion.AdminComment =
            string.IsNullOrWhiteSpace(request.AdminComment)
                ? null
                : request.AdminComment.Trim();

        suggestion.ReviewedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(suggestion));
    }

    /// <summary>
    /// Получает ID текущего пользователя из JWT.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }

    /// <summary>
    /// Преобразует сущность базы данных в DTO.
    /// </summary>
    private static ClubSuggestionDto ToDto(
        ClubSuggestion suggestion)
    {
        return new ClubSuggestionDto
        {
            Id = suggestion.Id,
            UserId = suggestion.UserId,
            UserName = suggestion.User.Name,
            Name = suggestion.Name,
            Description = suggestion.Description,
            InterestName = suggestion.InterestName,
            SuggestedAddress = suggestion.SuggestedAddress,
            Status = suggestion.Status,
            AdminComment = suggestion.AdminComment,
            CreatedAt = suggestion.CreatedAt,
            ReviewedAt = suggestion.ReviewedAt,
            CreatedClubId = suggestion.CreatedClubId
        };
    }

    /// <summary>
    /// Связывает одобренное предложение с созданным клубом.
    /// Доступно только администратору.
    /// </summary>
    [HttpPut("{id:guid}/link-club/{clubId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubSuggestionDto>> LinkCreatedClub(
        Guid id,
        Guid clubId,
        CancellationToken cancellationToken)
    {
        var suggestion = await _dbContext.ClubSuggestions
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (suggestion is null)
        {
            return NotFound(new
            {
                message = "Предложение не найдено."
            });
        }

        if (!string.Equals(
            suggestion.Status,
            "Approved",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Сначала нужно одобрить предложение."
            });
        }

        // Повторный запрос с тем же клубом считаем успешным.
        if (suggestion.CreatedClubId.HasValue)
        {
            if (suggestion.CreatedClubId.Value == clubId)
                return Ok(ToDto(suggestion));

            return Conflict(new
            {
                message = "Для этого предложения уже создан клуб."
            });
        }

        var clubExists = await _dbContext.Clubs
            .AnyAsync(x => x.Id == clubId, cancellationToken);

        if (!clubExists)
        {
            return NotFound(new
            {
                message = "Созданный клуб не найден."
            });
        }

        suggestion.CreatedClubId = clubId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(suggestion));
    }
}
