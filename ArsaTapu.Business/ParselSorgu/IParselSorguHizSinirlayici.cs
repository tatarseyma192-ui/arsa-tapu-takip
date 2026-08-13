namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// Requirements madde 5: "İstekler kontrollü/yavaş aralıklarla atılır (site engellememesi için)".
/// Singleton olarak kaydedilmelidir (DI) — sınırlama TÜM uygulama genelinde paylaşılmalı,
/// istek/oturum bazlı OLMAMALIDIR.
/// </summary>
public interface IParselSorguHizSinirlayici
{
    /// <summary>Son TKGM isteğinden bu yana geçen süre yeterli değilse, yeterli olana kadar bekler.</summary>
    Task BeklemeSuresinceBeklaAsync(CancellationToken ct = default);
}
