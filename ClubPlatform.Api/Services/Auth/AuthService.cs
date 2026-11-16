using BCrypt.Net;
using ClubPlatform.Api.Data;
using ClubPlatform.Api.Models;
using ClubPlatform.Api.Services.Jwt;
using ClubPlatform.Shared.DTOs.Auth;
using ClubPlatform.Shared.DTOs.Users;
using ClubPlatform.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClubPlatform.Api.Services.Auth;

/// <summary>
/// Реализация регистрации и авторизации пользователей.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtService _jwtService;

    public AuthService(
        AppDbContext dbContext,
        IJwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Регистрация пользователя.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Нормализуем email:
        // пробелы убираем, буквы переводим в нижний регистр.
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        // Проверяем, существует ли пользователь.
        var userExists = await _dbContext.Users
            .AnyAsync(
                user => user.Email.ToLower() == email,
                cancellationToken);

        if (userExists)
        {
            throw new InvalidOperationException(
                "Пользователь с таким email уже зарегистрирован.");
        }

        // Создаём BCrypt-хэш пароля.
        // Сам пароль в базу никогда не записываем.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(
            request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),

            Email = email,

            PasswordHash = passwordHash,

            // Обычная регистрация всегда создаёт User.
            // Создание Admin будет сделано отдельно.
            Role = UserRole.User,

            Name = request.Name.Trim(),

            Age = request.Age,

            Gender = request.Gender,

            PhotoUrl = request.PhotoUrl.Trim(),

            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateAuthResponse(user);
    }

    /// <summary>
    /// Авторизация пользователя.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                item => item.Email.ToLower() == email,
                cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Неверный email или пароль.");
        }

        // Проверяем введённый пароль
        // против BCrypt-хэша из базы.
        var passwordIsValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!passwordIsValid)
        {
            throw new UnauthorizedAccessException(
                "Неверный email или пароль.");
        }

        return CreateAuthResponse(user);
    }

    /// <summary>
    /// Создаёт JWT-ответ.
    /// </summary>
    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _jwtService.GenerateToken(user);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(120);

        return new AuthResponse
        {
            Token = token,

            ExpiresAt = expiresAt,

            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Age = user.Age,
                Gender = user.Gender,
                PhotoUrl = user.PhotoUrl,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            }
        };
    }
}