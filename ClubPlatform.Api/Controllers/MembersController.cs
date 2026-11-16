using System.Security.Claims;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Hubs;
using ClubPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ClubPlatform.Api.Services.Chat;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Вступление и выход пользователей из клубов.
/// </summary>
[ApiController]
[Route("api/clubs/{clubId:guid}/members")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    private readonly IHubContext<ClubChatHub> _hubContext;
    private readonly ClubConnectionTracker _connectionTracker;

    public MembersController(
        AppDbContext dbContext,
        IHubContext<ClubChatHub> hubContext,
        ClubConnectionTracker connectionTracker)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Вступает текущим пользователем в клуб.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Join(
        Guid clubId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        // -----------------------------------------------------
        // Администратор не вступает в клубы.
        // -----------------------------------------------------

        if (User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // -----------------------------------------------------
        // Проверяем клуб.
        // -----------------------------------------------------

        var club = await _dbContext.Clubs
            .FirstOrDefaultAsync(
                item => item.Id == clubId,
                cancellationToken);

        if (club is null)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }


        // Проверяем, не заблокирован ли пользователь в этом клубе.
        var isBanned = await _dbContext.ClubBans.AnyAsync(
            item => item.ClubId == clubId && item.UserId == userId,
            cancellationToken);

        if (isBanned)
        {
            return StatusCode(403, new
            {
                message = "Администратор запретил тебе вступать в этот клуб."
            });
        }


        // -----------------------------------------------------
        // Проверяем, не является ли пользователь
        // уже участником.
        // -----------------------------------------------------

        var existingMember =
            await _dbContext.ClubMembers
                .AnyAsync(
                    item =>
                        item.ClubId == clubId &&
                        item.UserId == userId,
                    cancellationToken);

        if (existingMember)
        {
            return Conflict(new
            {
                message = "Вы уже являетесь участником клуба."
            });
        }

        // -----------------------------------------------------
        // Проверяем лимит клуба.
        // -----------------------------------------------------

        if (club.MaxMembers.HasValue)
        {
            var membersCount =
                await _dbContext.ClubMembers
                    .CountAsync(
                        item => item.ClubId == clubId,
                        cancellationToken);

            if (membersCount >= club.MaxMembers.Value)
            {
                return Conflict(new
                {
                    message = "Клуб уже заполнен."
                });
            }
        }

        var member = new ClubMember
        {
            Id = Guid.NewGuid(),

            ClubId = clubId,

            UserId = userId,

            JoinedAt = DateTimeOffset.UtcNow,

            IsMuted = false
        };

        _dbContext.ClubMembers.Add(member);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Ok(new
        {
            message = "Вы успешно вступили в клуб."
        });
    }

    /// <summary>
    /// Выходит текущим пользователем из клуба.
    /// Добровольный выход не создаёт блокировку.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Leave(
        Guid clubId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        // Администратор не вступает в клубы и не выходит из них.
        if (User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Ищем участие пользователя в клубе.
        var member = await _dbContext.ClubMembers
            .FirstOrDefaultAsync(
                item =>
                    item.ClubId == clubId &&
                    item.UserId == userId,
                cancellationToken);

        if (member is null)
        {
            return NotFound(new
            {
                message = "Вы не являетесь участником этого клуба."
            });
        }

        // Удаляем только запись об участии.
        // Блокировка в ClubBans при добровольном выходе не создаётся.
        _dbContext.ClubMembers.Remove(member);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Вы успешно вышли из клуба."
        });
    }

    /// <summary>
    /// Возвращает список участников клуба.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMembers(
        Guid clubId,
        CancellationToken cancellationToken)
    {
        var clubExists =
            await _dbContext.Clubs.AnyAsync(
                club => club.Id == clubId,
                cancellationToken);

        if (!clubExists)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }

        var members =
            await _dbContext.ClubMembers
                .AsNoTracking()
                .Where(member => member.ClubId == clubId)
                .OrderBy(member => member.JoinedAt)
                .Select(member => new
                {
                    member.UserId,
                    member.User.Name,
                    member.User.PhotoUrl,
                    member.JoinedAt,
                    member.IsMuted
                })
                .ToListAsync(cancellationToken);

        return Ok(members);
    }


    /// <summary>
    /// Удаляет пользователя из клуба и принудительно
    /// исключает его активные подключения из SignalR-чата.
    /// Доступно только администратору.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveMember(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Проверяем существование клуба.
        var clubExists = await _dbContext.Clubs
            .AnyAsync(club => club.Id == clubId, cancellationToken);

        if (!clubExists)
        {
            return NotFound(new { message = "Клуб не найден." });
        }

        // Проверяем существование пользователя.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                item => item.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Пользователь не найден." });
        }

        // Нельзя удалить администратора из клуба.
        if (user.Role == ClubPlatform.Shared.Enums.UserRole.Admin)
        {
            return BadRequest(new
            {
                message = "Нельзя удалить администратора из клуба."
            });
        }

        // Ищем участие пользователя в клубе.
        var member = await _dbContext.ClubMembers
            .FirstOrDefaultAsync(
                item => item.ClubId == clubId && item.UserId == userId,
                cancellationToken);

        if (member is null)
        {
            return NotFound(new
            {
                message = "Этот пользователь не состоит в клубе."
            });
        }


        // Получаем ID администратора, который выполняет удаление.
        var adminId = GetCurrentUserId();

        // Проверяем, существует ли уже блокировка.
        var existingBan = await _dbContext.ClubBans
            .AnyAsync(
                item => item.ClubId == clubId
                        && item.UserId == userId,
                cancellationToken);

        // Создаём блокировку, если её ещё нет.
        if (!existingBan)
        {
            _dbContext.ClubBans.Add(new ClubBan
            {
                Id = Guid.NewGuid(),
                ClubId = clubId,
                UserId = userId,
                BannedByUserId = adminId,
                Reason = "Удалён администратором из клуба.",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Удаляем пользователя из участников клуба.
        _dbContext.ClubMembers.Remove(member);

        // Сохраняем удаление и блокировку одновременно.
        await _dbContext.SaveChangesAsync(cancellationToken);


        // Получаем все активные подключения пользователя к клубу.
        var connections = _connectionTracker.GetConnections(
            clubId,
            userId);

        var groupName = $"club:{clubId}";

        // Исключаем каждое подключение из SignalR-группы.
        foreach (var connectionId in connections)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(
                connectionId,
                groupName,
                cancellationToken);

            // Уведомляем приложение пользователя об исключении.
            await _hubContext.Clients.Client(connectionId)
                .SendAsync(
                    "RemovedFromClub",
                    clubId,
                    cancellationToken);

            // Удаляем подключение из нашего отслеживания.
            _connectionTracker.Remove(
                clubId,
                userId,
                connectionId);
        }

        return Ok(new
        {
            message = "Пользователь удалён из клуба.",
            clubId,
            userId
        });
    }


    /// <summary>
    /// Возвращает список пользователей, заблокированных в клубе.
    /// Доступно только администратору.
    /// </summary>
    [HttpGet("bans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBannedMembers(
        Guid clubId,
        CancellationToken cancellationToken)
    {
        // Проверяем существование клуба.
        var clubExists = await _dbContext.Clubs
            .AnyAsync(
                club => club.Id == clubId,
                cancellationToken);

        if (!clubExists)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }

        // Получаем все блокировки выбранного клуба.
        var bannedMembers = await _dbContext.ClubBans
            .AsNoTracking()
            .Where(ban => ban.ClubId == clubId)
            .OrderByDescending(ban => ban.CreatedAt)
            .Select(ban => new
            {
                ban.UserId,
                UserName = ban.User.Name,
                UserPhotoUrl = ban.User.PhotoUrl,
                ban.Reason,
                ban.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(bannedMembers);
    }

    /// <summary>
    /// Снимает блокировку пользователя в клубе.
    /// Доступно только администратору.
    /// </summary>
    [HttpDelete("bans/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnbanMember(
        Guid clubId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Проверяем существование клуба.
        var clubExists = await _dbContext.Clubs
            .AnyAsync(
                club => club.Id == clubId,
                cancellationToken);

        if (!clubExists)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }

        // Ищем блокировку конкретного пользователя.
        var ban = await _dbContext.ClubBans
            .FirstOrDefaultAsync(
                item =>
                    item.ClubId == clubId &&
                    item.UserId == userId,
                cancellationToken);

        if (ban is null)
        {
            return NotFound(new
            {
                message = "Блокировка пользователя не найдена."
            });
        }

        // Удаляем блокировку.
        _dbContext.ClubBans.Remove(ban);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Блокировка пользователя снята.",
            clubId,
            userId
        });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Не удалось определить пользователя.");
        }

        return userId;
    }
}