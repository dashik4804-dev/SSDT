
using ClubPlatform.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Получение публичной информации о пользователях.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Возвращает публичный профиль пользователя
    /// и список клубов, в которых он состоит.
    /// </summary>
    [HttpGet("{userId:guid}/profile")]
    public async Task<IActionResult> GetUserProfile(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Ищем пользователя.
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        // Получаем клубы, в которых состоит пользователь.
        var clubs = await _dbContext.ClubMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderByDescending(member => member.JoinedAt)
            .Select(member => new
            {
                member.Club.Id,
                member.Club.Name,
                member.Club.Description,
                member.Club.PhotoUrl,
                member.Club.Address
            })
            .ToListAsync(cancellationToken);

        // Возвращаем только публичные данные.
        // Email, пароль и роль пользователя не раскрываем.
        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Name,
                user.Age,
                Gender = user.Gender.ToString(),
                user.PhotoUrl
            },
            clubs
        });
    }
}
