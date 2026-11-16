
using System.Security.Claims;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Shared.DTOs.Support;
using ClubPlatform.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Контроллер обращений пользователей в службу поддержки.
/// </summary>
[ApiController]
[Route("api/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public SupportController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Создаёт новое обращение от текущего пользователя.
    /// </summary>
    [HttpPost("tickets")]
    public async Task<ActionResult<SupportTicketDto>> CreateTicket(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SupportTickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userName = await _dbContext.Users
            .Where(user => user.Id == userId.Value)
            .Select(user => user.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetMyTickets),
            new { },
            new SupportTicketDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                UserName = userName ?? string.Empty,
                Subject = ticket.Subject,
                Message = ticket.Message,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt
            });
    }

    /// <summary>
    /// Возвращает обращения текущего пользователя.
    /// Администратор также может просматривать свои обращения.
    /// </summary>
    [HttpGet("tickets/my")]
    public async Task<ActionResult<List<SupportTicketDto>>> GetMyTickets(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var tickets = await _dbContext.SupportTickets
            .AsNoTracking()
            .Where(ticket => ticket.UserId == userId.Value)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new SupportTicketDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                UserName = ticket.User.Name,
                Subject = ticket.Subject,
                Message = ticket.Message,
                Status = ticket.Status,
                AdminResponse = ticket.AdminResponse,
                CreatedAt = ticket.CreatedAt,
                AnsweredAt = ticket.AnsweredAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tickets);
    }

    /// <summary>
    /// Возвращает все обращения. Доступно только администратору.
    /// </summary>
    [HttpGet("tickets")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<SupportTicketDto>>> GetAllTickets(
        CancellationToken cancellationToken)
    {
        var tickets = await _dbContext.SupportTickets
            .AsNoTracking()
            .OrderBy(ticket => ticket.Status == "Pending" ? 0 : 1)
            .ThenByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new SupportTicketDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                UserName = ticket.User.Name,
                Subject = ticket.Subject,
                Message = ticket.Message,
                Status = ticket.Status,
                AdminResponse = ticket.AdminResponse,
                CreatedAt = ticket.CreatedAt,
                AnsweredAt = ticket.AnsweredAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tickets);
    }

    /// <summary>
    /// Отправляет ответ на обращение. Доступно только администратору.
    /// </summary>

    [HttpPut("tickets/{ticketId:guid}/response")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RespondToTicket(
        Guid ticketId,
        [FromBody] RespondSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .Include(item => item.User)
            .FirstOrDefaultAsync(
                item => item.Id == ticketId,
                cancellationToken);

        if (ticket == null)
            return NotFound("Обращение не найдено.");

        ticket.AdminResponse = request.Response.Trim();
        ticket.Status = "Answered";
        ticket.AnsweredAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Возвращаем обновлённое обращение в формате JSON.
        var result = new SupportTicketDto
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            UserName = ticket.User.Name,
            Subject = ticket.Subject,
            Message = ticket.Message,
            Status = ticket.Status,
            AdminResponse = ticket.AdminResponse,
            CreatedAt = ticket.CreatedAt,
            AnsweredAt = ticket.AnsweredAt
        };

        return Ok(result);
    }


    /// <summary>
    /// Получает идентификатор текущего пользователя из JWT.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(userIdValue, out var userId))
            return userId;

        return null;
    }
}
