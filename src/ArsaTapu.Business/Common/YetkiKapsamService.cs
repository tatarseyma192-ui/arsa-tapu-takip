using ArsaTapu.Domain.Entities;

namespace ArsaTapu.Business.Common;

/// <summary>
/// Requirements madde 1'deki veri kapsamı kurallarını (özellikle Patron'un yalnızca
/// kendi verisini görebilmesi) merkezi olarak uygular. Controller'lara dağıtılmaz;
/// rol bazlı erişim [Authorize(Policy=...)] ile, kayıt bazlı kapsam burada kontrol edilir.
/// </summary>
public interface IYetkiKapsamService
{
    /// <summary>Giriş yapan kullanıcı Patron rolündeyse kendi KisiId'sini döner, değilse null.</summary>
    Task<int?> PatronKisiIdGetirAsync(CancellationToken ct = default);

    /// <summary>Patron, kendi KisiId'si dışında bir kayda erişmeye çalışıyorsa YetkisizErisimException fırlatır.</summary>
    Task KisiErisimKontrolEtAsync(int kisiId, CancellationToken ct = default);

    /// <summary>
    /// Taşınmaz silme yetkisi: Admin her zaman silebilir; Personel yalnızca kendi
    /// yüklediği kayda bağlı taşınmazları silebilir (Requirements madde 1 rol tablosu).
    /// </summary>
    Task TasinmazSilYetkisiKontrolEtAsync(ArsaTapu.Domain.Entities.Tasinmaz tasinmaz, CancellationToken ct = default);
}
