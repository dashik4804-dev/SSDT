using ClubPlatform.Api.Services.Cloudinary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubPlatform.Api.Controllers;

/// <summary>
/// Работа с изображениями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly ICloudinaryService _cloudinaryService;

    public ImagesController(
        ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Загружает фотографию в Cloudinary.
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                await _cloudinaryService.UploadImageAsync(
                    file,
                    "club-platform/users",
                    cancellationToken);

            return Ok(new
            {
                url
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }
    }
    /// <summary>
    /// Загрузка фотографии для ещё не зарегистрированного пользователя.
    /// Этот endpoint не требует JWT.
    /// </summary>
    [HttpPost("registration-upload")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadRegistrationPhoto(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = await _cloudinaryService.UploadImageAsync(
                file,
                "club-platform/users",
                cancellationToken);

            return Ok(new
            {
                url
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(500, new
            {
                message = exception.Message
            });
        }
    }
}