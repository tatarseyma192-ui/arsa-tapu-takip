using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.ParselKml;

namespace ArsaTapu.Business.ParselSorgu;

/// <summary>Requirements madde 4, 4.2, 4.3, 5 — Parsel Sorgu (TKGM) otomasyon modülünün orkestratörü.</summary>
public interface IParselKmlService
{
    Task<PagedResult<ParselKmlDto>> ListeleAsync(PagedRequest istek, CancellationToken ct = default);

    /// <summary>Tek parsel sorgusu — hem ilk sorgu hem de "Tekrar dene" için kullanılır.</summary>
    Task<ParselSorguSonucuDto> SorgulaAsync(ParselSorguIstegiDto istek, CancellationToken ct = default);

    /// <summary>
    /// Birden fazla parseli SIRAYLA (rate limiter aralığıyla) sorgular. Mevcut
    /// IKmlTekillestirmeService üzerinden zaten çekilmiş olanları atlar.
    /// </summary>
    Task<TopluParselSorguSonucuDto> TopluSorgulaAsync(TopluParselSorguIstegiDto istek, CancellationToken ct = default);

    /// <summary>Requirements madde 2.1/5: kullanıcının kendi indirdiği KML'i manuel yüklemesi.</summary>
    Task<ParselKmlDto> ManuelYukleAsync(
        ParselSorguIstegiDto parselBilgisi, byte[] kmlIcerigi, CancellationToken ct = default);

    /// <summary>Requirements madde 4.3: silinen kayıt, aynı Ada/Parsel tekrar geldiğinde yeniden sorgulanabilir olur.</summary>
    Task SilAsync(int id, CancellationToken ct = default);
}
