using ClubPlatform.Api.Services.Interests;
using ClubPlatform.Shared.DTOs.Interests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Управление интересами.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InterestsController : ControllerBase
{
    private readonly IInterestService _interestService;

    public InterestsController(
        IInterestService interestService)
    {
        _interestService = interestService;
    }

    // =====================================================
    // GET
    // Доступен обычному пользователю и администратору.
    // =====================================================

    /// <summary>
    /// Возвращает список всех интересов.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<InterestDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var interests =
            await _interestService.GetAllAsync(
                cancellationToken);

        return Ok(interests);
    }

    /// <summary>
    /// Возвращает один интерес.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<InterestDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var interest =
            await _interestService.GetByIdAsync(
                id,
                cancellationToken);

        if (interest is null)
        {
            return NotFound(new
            {
                message = "Интерес не найден."
            });
        }

        return Ok(interest);
    }

    // =====================================================
    // POST
    // Только администратор.
    // =====================================================

    /// <summary>
    /// Создаёт новый интерес.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InterestDto>> Create(
        [FromBody] CreateInterestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var interest =
                await _interestService.CreateAsync(
                    request,
                    cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = interest.Id
                },
                interest);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // =====================================================
    // PUT
    // Только администратор.
    // =====================================================

    /// <summary>
    /// Изменяет существующий интерес.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InterestDto>> Update(
        Guid id,
        [FromBody] UpdateInterestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var interest =
                await _interestService.UpdateAsync(
                    id,
                    request,
                    cancellationToken);

            if (interest is null)
            {
                return NotFound(new
                {
                    message = "Интерес не найден."
                });
            }

            return Ok(interest);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // =====================================================
    // DELETE
    // Только администратор.
    // =====================================================

    /// <summary>
    /// Удаляет интерес.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted =
            await _interestService.DeleteAsync(
                id,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Интерес не найден."
            });
        }

        return NoContent();
    }
}