using Microsoft.AspNetCore.Identity;

namespace ArsaTapu.DataAccess.Identity;

/// <summary>ASP.NET Identity kullanıcı modeli. JWT + rol bazlı yetkilendirme bu üzerinden çalışır.</summary>
public class ApplicationUser : IdentityUser
{
    public string? AdSoyad { get; set; }
}
