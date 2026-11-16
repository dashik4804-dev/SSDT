using System.Security.Claims;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Services.Chat;
using ClubPlatform.Shared.DTOs.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// История сообщений клубов.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly AppDbContext _dbContext;

    public MessagesController(
    IMessageService messageService,
    AppDbContext dbContext)
    {
        _messageService = messageService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Получает полную историю сообщений клуба.
    /// </summary>
    [HttpGet("club/{clubId:guid}")]
    public async Task<ActionResult<List<MessageDto>>> GetHistory(
        Guid clubId,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
    ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // Администратор может просматривать чат
        // для целей модерации, даже не являясь участником.
        if (!User.IsInRole("Admin"))
        {
            var isMember = await _dbContext.ClubMembers
                .AnyAsync(
                    member =>
                        member.ClubId == clubId &&
                        member.UserId == userId,
                    cancellationToken);

            if (!isMember)
            {
                return Forbid();
            }
        }

        var messages =
            await _messageService.GetHistoryAsync(
                clubId,
                cancellationToken);

        return Ok(messages);
    }
}