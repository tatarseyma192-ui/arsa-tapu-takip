using ArsaTapu.Dto.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

/// <summary>
/// Varsayılan olarak tüm endpoint'ler kimlik doğrulaması ister; anonim erişim
/// gereken uçlar (login/refresh) [AllowAnonymous] ile işaretlenir.
/// Yanıtlar her zaman Technical Defaults madde 4'teki standart zarf ile döner.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult Basarili<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult BasariliMesaj(string? message = null) =>
        Ok(ApiResponse.Ok(message));

    protected IActionResult Olusturuldu<T>(string actionName, object routeValues, T data) =>
        CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data));

    protected IActionResult Hatali(string message, int statusCode = StatusCodes.Status400BadRequest, List<FieldError>? errors = null) =>
        StatusCode(statusCode, ApiResponse.Fail(message, errors));
}
