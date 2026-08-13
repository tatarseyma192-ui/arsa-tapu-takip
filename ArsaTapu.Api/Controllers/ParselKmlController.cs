using ArsaTapu.Api.Authorization;
using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.ParselKml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

/// <summary>
/// Requirements madde 5: Parsel Sorgu (TKGM) otomasyon modülü. Yalnızca Admin/Personel
/// erişebilir (Requirements madde 1 — Patron read-only, KML işlemleriyle ilgilenmez).
///
/// ÖNEMLİ — İKİ YOL, FARKLI GÜVEN SEVİYESİ:
///   1) ManuelYukle: BİRİNCİL / GÜVENİLİR yol. Kullanıcı kendi indirdiği KML'i yükler.
///   2) Sorgula / TopluSorgula: İKİNCİL yol. TKGM entegrasyonu gerçek HAR (network trafiği)
///      kaydından doğrulandı ve sahte HTTP handler ile test edildi — ama gerçek TKGM sunucusuna
///      hiç bağlanılmadı (bkz. STATUS.md). Sorun görülürse appsettings'teki ParselSorgu:DeneyselModu
///      kod değişikliği olmadan tekrar açılıp "Doğrulanmadı" uyarısı geri getirilebilir.
/// </summary>
[Route("api/parsel-kml")]
[Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
public class ParselKmlController : BaseApiController
{
    private readonly IParselKmlService _parselKmlService;
    private readonly IDosyaDogrulamaService _dosyaDogrulama;

    public ParselKmlController(IParselKmlService parselKmlService, IDosyaDogrulamaService dosyaDogrulama)
    {
        _parselKmlService = parselKmlService;
        _dosyaDogrulama = dosyaDogrulama;
    }

    /// <summary>Liste her kayıtta Kaynak ("Otomatik"/"Manuel") ve Deneysel alanlarını içerir.</summary>
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] PagedRequest istek, CancellationToken ct)
    {
        var sonuc = await _parselKmlService.ListeleAsync(istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>
    /// BİRİNCİL / GÜVENİLİR yol: kullanıcının kendi indirdiği KML dosyasını yükler. TKGM
    /// entegrasyonu doğrulanana kadar bu yöntem tercih edilmelidir.
    /// </summary>
    [HttpPost("manuel-yukle")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ManuelYukle([FromForm] ParselKmlManuelYuklemeIstegi istek, CancellationToken ct)
    {
        _dosyaDogrulama.KmlDogrula(istek.Dosya.FileName, istek.Dosya.Length);

        using var akis = new MemoryStream();
        await istek.Dosya.CopyToAsync(akis, ct);

        var parselBilgisi = new ParselSorguIstegiDto
        {
            Il = istek.Il,
            Ilce = istek.Ilce,
            Mahalle = istek.Mahalle,
            Ada = istek.Ada,
            Parsel = istek.Parsel
        };

        var sonuc = await _parselKmlService.ManuelYukleAsync(parselBilgisi, akis.ToArray(), ct);
        return Basarili(sonuc, "KML dosyası manuel olarak yüklendi.");
    }

    /// <summary>
    /// İKİNCİL / DENEYSEL yol: tek parsel için TKGM'den otomatik sorgu. Aynı uç, başarısız bir
    /// kaydın "Tekrar dene"si için de kullanılır. Dönen sonuç HER ZAMAN Deneysel=true ve
    /// "Doğrulanmadı, kontrol edin." uyarısı taşır — başarılı olsa bile.
    /// </summary>
    [HttpPost("sorgula")]
    public async Task<IActionResult> Sorgula([FromBody] ParselSorguIstegiDto istek, CancellationToken ct)
    {
        var sonuc = await _parselKmlService.SorgulaAsync(istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>
    /// İKİNCİL / DENEYSEL yol: birden fazla parseli sırayla sorgular (Requirements madde 5:
    /// hız sınırlayıcı aralığıyla). Zaten başarıyla çekilmiş parseller mevcut
    /// IKmlTekillestirmeService üzerinden atlanır.
    /// </summary>
    [HttpPost("toplu-sorgula")]
    public async Task<IActionResult> TopluSorgula([FromBody] TopluParselSorguIstegiDto istek, CancellationToken ct)
    {
        var sonuc = await _parselKmlService.TopluSorgulaAsync(istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>Requirements madde 4.3: silinen kayıt, aynı Ada/Parsel tekrar geldiğinde yeniden sorgulanabilir olur.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id, CancellationToken ct)
    {
        await _parselKmlService.SilAsync(id, ct);
        return BasariliMesaj("KML kaydı silindi; bu parsel tekrar sorgulanabilir hale geldi.");
    }
}
