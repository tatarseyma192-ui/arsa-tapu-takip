namespace ArsaTapu.Domain.Exceptions;

/// <summary>
/// Kimliği doğrulanmış ama kaynağa erişim yetkisi olmayan kullanıcı (ör. Patron'un
/// başka bir kişinin verisine erişmeye çalışması). Business katmanında fırlatılır,
/// ExceptionHandlingMiddleware tarafından 403'e çevrilir.
/// </summary>
public class YetkisizErisimException : Exception
{
    public YetkisizErisimException(string? message = null)
        : base(message ?? "Bu kaynağa erişim yetkiniz yok.") { }
}
