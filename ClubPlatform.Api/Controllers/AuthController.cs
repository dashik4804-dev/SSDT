using ClubPlatform.Api.Services.Auth;
using ClubPlatform.Api.Data;
using ClubPlatform.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Регистрация и авторизация пользователей.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _dbContext;

    public AuthController(
        IAuthService authService,
        AppDbContext dbContext)
    {
        _authService = authService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    /// <summary>
    /// Вход пользователя.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Неверный email или пароль."
            });
        }
    }

    /// <summary>
    /// Возвращает полную информацию о текущем пользователе.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(
        CancellationToken cancellationToken)
    {
        // Получаем ID текущего пользователя из JWT-токена.
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        // Проверяем, удалось ли прочитать корректный GUID.
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Не удалось определить пользователя по токену."
            });
        }

        // Ищем пользователя в PostgreSQL.
        // AsNoTracking подходит для чтения, поскольку запись не изменяем.
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        // Возвращаем только данные профиля.
        // Хэш пароля и другие внутренние поля не передаём.
        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            age = user.Age,
            gender = user.Gender.ToString(),
            photoUrl = user.PhotoUrl,
            role = user.Role.ToString()
        });
    }


    /// <summary>
    /// Обновляет разрешённые поля профиля текущего пользователя.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        // Находим ID текущего пользователя в JWT.
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Не удалось определить пользователя."
            });
        }

        // Получаем пользователя из PostgreSQL.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        // Проверяем, что выбрано допустимое значение пола.
        if (!Enum.IsDefined(request.Gender))
        {
            return BadRequest(new
            {
                message = "Указано недопустимое значение пола."
            });
        }

        // Меняем только поля, доступные для редактирования.
        user.Name = request.Name.Trim();
        user.Age = request.Age;
        user.Gender = request.Gender;

        // Если фотография передана, обновляем URL.
        // Null оставляем как null, если фотографии у пользователя нет.
        user.PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl)
            ? null
            : request.PhotoUrl.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Профиль успешно обновлён.",
            name = user.Name,
            age = user.Age,
            gender = user.Gender.ToString(),
            photoUrl = user.PhotoUrl
        });
    }

}