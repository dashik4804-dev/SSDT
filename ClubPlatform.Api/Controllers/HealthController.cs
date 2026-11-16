using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Проверка доступности Backend.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Возвращает состояние API.
    /// Авторизация не требуется.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            message = "ClubPlatform API работает!",
            serverTime = DateTimeOffset.UtcNow
        });
    }
}