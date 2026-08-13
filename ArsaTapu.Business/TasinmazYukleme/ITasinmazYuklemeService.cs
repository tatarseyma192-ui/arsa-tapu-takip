using ArsaTapu.Dto.Tekillestirme;
using ArsaTapu.Dto.TasinmazYukleme;

namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// PDF/Excel yükleme akışının orkestratörü (Requirements madde 2, 3, 4.1).
/// Önizleme adımları HİÇBİR veritabanı yazımı yapmaz; yalnızca OnaylaVeIsleAsync yazar.
/// </summary>
public interface ITasinmazYuklemeService
{
    Task<TasinmazOnizlemeSonucuDto> PdfOnizlemeOlusturAsync(
        int kisiId, Stream pdfStream, string dosyaAdi, CancellationToken ct = default);

    Task<TasinmazOnizlemeSonucuDto> ExcelOnizlemeOlusturAsync(
        int kisiId, Stream excelStream, string dosyaAdi, CancellationToken ct = default);

    /// <summary>
    /// Karşılaştırma motoru: Yeni Alım tespiti + Satıldı tespiti (madde 3) + YuklemeKaydi
    /// oluşturma + KML sorgulanması gereken parsel listesi (madde 5 — TKGM'ye DOKUNULMAZ).
    /// </summary>
    Task<TasinmazOnaySonucuDto> OnaylaVeIsleAsync(
        TasinmazOnayIstegiDto istek, string yukleyenKullaniciId, CancellationToken ct = default);

    /// <summary>Requirements madde 2.1: indirilebilir Excel üretimi.</summary>
    byte[] OnizlemeyiExceleAktar(IReadOnlyList<MulkiyetAdayDto> satirlar);
}
