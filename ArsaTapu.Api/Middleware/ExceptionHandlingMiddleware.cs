using System.Net;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Common;

namespace ArsaTapu.Api.Middleware;

/// <summary>
/// Teknik hata detayları (stack trace, exception mesajı) asla kullanıcıya gösterilmez
/// (Handbook madde 9). Beklenmeyen hatalar loglanır, kullanıcıya genel mesaj döner.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HataYanitiYazAsync(context, ex);
        }
    }

    private async Task HataYanitiYazAsync(HttpContext context, Exception ex)
    {
        var (statusCode, mesaj) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            BusinessRuleException => (HttpStatusCode.BadRequest, ex.Message),
            YetkisizErisimException => (HttpStatusCode.Forbidden, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(ex, "Beklenmeyen hata: {Yol}", context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(mesaj));
    }
}
