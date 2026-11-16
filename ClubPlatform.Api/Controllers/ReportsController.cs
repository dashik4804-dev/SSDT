using System.Security.Claims;
using ClubPlatform.Api.Services.Moderation;
using ClubPlatform.Shared.DTOs.Reports;
using ClubPlatform.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Жалобы пользователей и модерация.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public ReportsController(
        IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    // =========================================================
    // USER
    // Создание жалобы.
    // =========================================================

    /// <summary>
    /// Создаёт жалобу на сообщение.
    /// Доступно обычным пользователям.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReportDto>> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var report =
                await _moderationService.CreateReportAsync(
                    request.MessageId,
                    userId,
                    request.Reason,
                    cancellationToken);

            return Ok(report);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }


    /// <summary>
    /// Получает жалобы, отправленные текущим пользователем.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<ReportDto>>> GetMyReports(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var reports = await _moderationService.GetMyReportsAsync(
            userId,
            cancellationToken);

        return Ok(reports);
    }


    // =========================================================
    // ADMIN
    // Список жалоб.
    // =========================================================

    /// <summary>
    /// Получает список жалоб.
    /// Только администратор.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ReportDto>>> GetReports(
        [FromQuery] ReportStatus? status,
        CancellationToken cancellationToken)
    {
        var reports =
            await _moderationService.GetReportsAsync(
                status,
                cancellationToken);

        return Ok(reports);
    }

    /// <summary>
    /// Возвращает список пользователей с активным ограничением.
    /// Только администратор.
    /// </summary>
    [HttpGet("muted-users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<MutedClubMemberDto>>> GetMutedUsers(
        CancellationToken cancellationToken)
    {
        var mutedMembers =
            await _moderationService.GetMutedMembersAsync(
                cancellationToken);

        return Ok(mutedMembers);
    }

    // =========================================================
    // ADMIN
    // Изменение статуса жалобы.
    // =========================================================

    /// <summary>
    /// Изменяет статус жалобы.
    /// </summary>
    /// <summary>
    /// Отклоняет новую жалобу.
    /// Только администратор.
    /// </summary>
    [HttpPut("{reportId:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> UpdateStatus(
        Guid reportId,
        [FromBody] UpdateReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var report =
                await _moderationService.UpdateReportStatusAsync(
                    reportId,
                    request.Status,
                    cancellationToken);

            if (report is null)
            {
                return NotFound(new
                {
                    message = "Жалоба не найдена."
                });
            }

            return Ok(report);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // =========================================================
    // ADMIN
    // Удаление сообщения.
    // =========================================================

    /// <summary>
    /// Мягко удаляет сообщение.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMessage(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var deleted =
            await _moderationService.DeleteMessageAsync(
                messageId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Сообщение не найдено."
            });
        }

        return Ok(new
        {
            message = "Сообщение удалено."
        });
    }

    // =========================================================
    // ADMIN
    // MUTE / UNMUTE
    // =========================================================

    /// <summary>
    /// Устанавливает или снимает Mute
    /// с пользователя в конкретном клубе.
    /// </summary>
    [HttpPut("clubs/{clubId:guid}/users/{userId:guid}/mute")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetMute(
        Guid clubId,
        Guid userId,
        [FromQuery] bool muted,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _moderationService.SetMuteAsync(
                    clubId,
                    userId,
                    muted,
                    cancellationToken);

            if (!result)
            {
                return NotFound(new
                {
                    message =
                        "Пользователь не является участником клуба."
                });
            }

            return Ok(new
            {
                message = muted
                    ? "Пользователь заглушён."
                    : "Mute снят.",

                userId,

                clubId,

                isMuted = muted
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}