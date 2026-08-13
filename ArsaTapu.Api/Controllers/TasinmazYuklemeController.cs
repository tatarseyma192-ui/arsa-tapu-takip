using ArsaTapu.Api.Authorization;
using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Tekillestirme;
using ArsaTapu.Dto.TasinmazYukleme;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

/// <summary>
/// Requirements madde 2 + 4.1: PDF/Excel önizleme + onay + karşılaştırma motoru.
/// Yalnızca Admin/Personel yükleme yapabilir (Requirements madde 1 — Patron read-only).
/// </summary>
[Route("api/tasinmaz-yukleme")]
[Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
public class TasinmazYuklemeController : BaseApiController
{
    private readonly ITasinmazYuklemeService _yuklemeService;
    private readonly IDosyaDogrulamaService _dosyaDogrulama;
    private readonly ICurrentUserService _currentUser;

    public TasinmazYuklemeController(
        ITasinmazYuklemeService yuklemeService,
        IDosyaDogrulamaService dosyaDogrulama,
        ICurrentUserService currentUser)
    {
        _yuklemeService = yuklemeService;
        _dosyaDogrulama = dosyaDogrulama;
        _currentUser = currentUser;
    }

    /// <summary>Requirements madde 2.1: PDF önizleme — HİÇBİR veritabanı yazımı yapılmaz.</summary>
    [HttpPost("onizleme/pdf")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> PdfOnizleme([FromForm] DosyaYuklemeIstegi istek, CancellationToken ct)
    {
        var ilkBaytlar = await IlkBaytlariOkuAsync(istek.Dosya, ct);
        _dosyaDogrulama.Dogrula(istek.Dosya.FileName, istek.Dosya.Length, ilkBaytlar, KaynakTuru.Pdf);

        await using var akis = istek.Dosya.OpenReadStream();
        var sonuc = await _yuklemeService.PdfOnizlemeOlusturAsync(istek.KisiId, akis, istek.Dosya.FileName, ct);
        return Basarili(sonuc);
    }

    /// <summary>Requirements madde 2.2: Doğrudan Excel yükleme — aynı önizleme + onay akışı.</summary>
    [HttpPost("onizleme/excel")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> ExcelOnizleme([FromForm] DosyaYuklemeIstegi istek, CancellationToken ct)
    {
        var ilkBaytlar = await IlkBaytlariOkuAsync(istek.Dosya, ct);
        _dosyaDogrulama.Dogrula(istek.Dosya.FileName, istek.Dosya.Length, ilkBaytlar, KaynakTuru.Excel);

        await using var akis = istek.Dosya.OpenReadStream();
        var sonuc = await _yuklemeService.ExcelOnizlemeOlusturAsync(istek.KisiId, akis, istek.Dosya.FileName, ct);
        return Basarili(sonuc);
    }

    /// <summary>
    /// Karşılaştırma motoru (Requirements madde 3 + 4.1): Yeni Alım / Satıldı tespiti,
    /// YuklemeKaydi oluşturma, KML sorgulanması gereken parsel listesi üretimi.
    /// </summary>
    [HttpPost("onayla")]
    public async Task<IActionResult> Onayla([FromBody] TasinmazOnayIstegiDto istek, CancellationToken ct)
    {
        var yukleyenKullaniciId = _currentUser.UserId
            ?? throw new YetkisizErisimException("Kullanıcı kimliği doğrulanamadı.");

        var sonuc = await _yuklemeService.OnaylaVeIsleAsync(istek, yukleyenKullaniciId, ct);
        return Basarili(sonuc, "Yükleme başarıyla işlendi.");
    }

    /// <summary>Requirements madde 2.1: Önizlemedeki (veya onaylanan) satırların indirilebilir Excel çıktısı.</summary>
    [HttpPost("excel-indir")]
    public IActionResult ExceleAktar([FromBody] List<MulkiyetAdayDto> satirlar)
    {
        var dosyaBaytlari = _yuklemeService.OnizlemeyiExceleAktar(satirlar);
        return File(
            dosyaBaytlari,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "tasinmazlar.xlsx");
    }

    private static async Task<byte[]> IlkBaytlariOkuAsync(IFormFile dosya, CancellationToken ct)
    {
        var boyut = (int)Math.Min(dosya.Length, 8);
        var bayt = new byte[boyut];

        await using var akis = dosya.OpenReadStream();
        var okunan = 0;
        while (okunan < boyut)
        {
            var buAdimda = await akis.ReadAsync(bayt.AsMemory(okunan, boyut - okunan), ct);
            if (buAdimda == 0) break;
            okunan += buAdimda;
        }

        return bayt;
    }
}
