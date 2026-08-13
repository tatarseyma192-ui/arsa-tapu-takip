using System.Security.Claims;
using ArsaTapu.Domain.Common;

namespace ArsaTapu.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? KullaniciAdi =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public IReadOnlyList<string> Roller =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? new List<string>();

    public bool RoldeMi(string rol) => Roller.Contains(rol, StringComparer.OrdinalIgnoreCase);
}
