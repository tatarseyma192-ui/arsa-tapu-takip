namespace ArsaTapu.Domain.Common;

/// <summary>
/// Giriş yapan kullanıcı bilgisine erişim (audit alanları ve yetki kapsamı için).
/// Implementasyonu Api katmanında (HttpContext üzerinden) yapılır; arayüz Domain'de
/// tutulur ki DataAccess ve Business katmanları Api'ye bağımlı olmasın.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? KullaniciAdi { get; }
    IReadOnlyList<string> Roller { get; }
    bool RoldeMi(string rol);
}
