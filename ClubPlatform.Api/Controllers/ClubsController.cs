using System.Security.Claims;
using ClubPlatform.Api.Services.Clubs;
using ClubPlatform.Shared.DTOs.Clubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Работа с клубами.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClubsController : ControllerBase
{
    private readonly IClubService _clubService;

    public ClubsController(IClubService clubService)
    {
        _clubService = clubService;
    }

    // =====================================================
    // GET /api/clubs
    // =====================================================

    /// <summary>
    /// Возвращает все клубы.
    ///
    /// Если переданы interestIds, показываются клубы,
    /// содержащие хотя бы один выбранный интерес.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ClubDto>>> GetAll(
        [FromQuery] List<Guid>? interestIds,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        var clubs =
            await _clubService.GetAllAsync(
                interestIds,
                currentUserId,
                cancellationToken);

        return Ok(clubs);
    }

    // =====================================================
    // GET /api/clubs/{id}
    // =====================================================

    /// <summary>
    /// Возвращает один клуб.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClubDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        var club =
            await _clubService.GetByIdAsync(
                id,
                currentUserId,
                cancellationToken);

        if (club is null)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }

        return Ok(club);
    }

    // =====================================================
    // POST /api/clubs
    // Только Admin.
    // =====================================================

    /// <summary>
    /// Создаёт новый клуб.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubDto>> Create(
        [FromBody] CreateClubRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var club =
                await _clubService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = club.Id
                },
                club);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // =====================================================
    // PUT /api/clubs/{id}
    // Только Admin.
    // =====================================================

    /// <summary>
    /// Изменяет клуб.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClubDto>> Update(
        Guid id,
        [FromBody] UpdateClubRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var club =
                await _clubService.UpdateAsync(
                    id,
                    request,
                    cancellationToken);

            if (club is null)
            {
                return NotFound(new
                {
                    message = "Клуб не найден."
                });
            }

            return Ok(club);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // =====================================================
    // DELETE /api/clubs/{id}
    // Только Admin.
    // =====================================================

    /// <summary>
    /// Удаляет клуб.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted =
            await _clubService.DeleteAsync(
                id,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Клуб не найден."
            });
        }

        return NoContent();
    }

    // =====================================================
    // Вспомогательный метод
    // =====================================================

    /// <summary>
    /// Получает ID текущего пользователя из JWT.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (Guid.TryParse(value, out var userId))
        {
            return userId;
        }

        return null;
    }
}