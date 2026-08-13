using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// TKGM Parsel Sorgu istemcisi — GERÇEK, doğrulanmış API yapısına göre yazıldı (2026-08-04
/// tarihli gerçek network trafiği/HAR kaydından çıkarıldı).
///
/// Taban adres: https://cbsapi.tkgm.gov.tr/megsiswebapi.v3.1/api (appsettings'ten yapılandırılır)
/// Akış:
///   1. İl adı  -> Id       : IlKodlari (SABİT tablo, 81 il — HAR'dan doğrulandı)
///   2. İlçe adı -> Id      : GET /idariYapi/ilceListe/{ilId}      (önbelleklenir — ITkgmIdCache)
///   3. Mahalle adı -> Id   : GET /idariYapi/mahalleListe/{ilceId} (önbelleklenir — ITkgmIdCache)
///   4. Parsel doğrulama    : GET /parsel/{mahalleId}/{ada}/{parsel}
///   5. KML indirme         : GET /parsel/download/{mahalleId}/{ada}/{parsel}/kml
/// Kimlik doğrulama/cookie/API key GEREKMİYOR (HAR'da doğrulandı); yalnızca Referer header'ı
/// gönderilir (Program.cs'te AddHttpClient ile sabit ayarlanır).
///
/// Handbook madde 4: TKGM'e bağımlı TEK sınıf budur. Site yapısı değişirse yalnızca bu dosya
/// (ve appsettings'teki TkgmParselSorgu bölümü) güncellenir.
/// </summary>
public class TkgmParselSorguIstemcisi : IParselSorguIstemcisi
{
    private readonly HttpClient _httpClient;
    private readonly IParselSorguHizSinirlayici _hizSinirlayici;
    private readonly ITkgmIdCache _idCache;
    private readonly ILogger<TkgmParselSorguIstemcisi> _logger;

    public TkgmParselSorguIstemcisi(
        HttpClient httpClient,
        IParselSorguHizSinirlayici hizSinirlayici,
        ITkgmIdCache idCache,
        ILogger<TkgmParselSorguIstemcisi> logger)
    {
        _httpClient = httpClient;
        _hizSinirlayici = hizSinirlayici;
        _idCache = idCache;
        _logger = logger;
    }

    public async Task<ParselSorguIstemciSonucu> SorgulaAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default)
    {
        try
        {
            var ilId = IlKodlari.Bul(il);
            if (ilId is null)
                return ParselSorguIstemciSonucu.BasarisizSonuc($"'{il}' TKGM il listesinde bulunamadı.");

            var ilceId = await IlceIdCozAsync(ilId.Value, ilce, ct);
            if (ilceId is null)
                return ParselSorguIstemciSonucu.BasarisizSonuc($"'{ilce}' ilçesi TKGM'de '{il}' ili altında bulunamadı.");

            var mahalleId = await MahalleIdCozAsync(ilceId.Value, mahalle, ct);
            if (mahalleId is null)
                return ParselSorguIstemciSonucu.BasarisizSonuc($"'{mahalle}' mahallesi TKGM'de '{ilce}' ilçesi altında bulunamadı.");

            var parselVarMi = await ParselDogrulaAsync(mahalleId.Value, ada, parsel, ct);
            if (!parselVarMi)
                return ParselSorguIstemciSonucu.BasarisizSonuc("TKGM'de bu Ada/Parsel için kayıt bulunamadı.");

            var kmlBaytlari = await KmlIndirAsync(mahalleId.Value, ada, parsel, ct);
            if (kmlBaytlari is null || kmlBaytlari.Length == 0)
                return ParselSorguIstemciSonucu.BasarisizSonuc("TKGM'den KML dosyası indirilemedi.");

            return ParselSorguIstemciSonucu.BasariliSonuc(kmlBaytlari);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TKGM sorgusu sırasında beklenmeyen hata: {Il}/{Ilce}/{Mahalle} Ada {Ada} Parsel {Parsel}",
                il, ilce, mahalle, ada, parsel);

            return ParselSorguIstemciSonucu.BasarisizSonuc(
                "TKGM servisine ulaşılamadı veya beklenmeyen bir hata oluştu.");
        }
    }

    private async Task<int?> IlceIdCozAsync(int ilId, string ilceAdi, CancellationToken ct)
    {
        var liste = _idCache.IlceListesiGetir(ilId);
        if (liste is null)
        {
            await _hizSinirlayici.BeklemeSuresinceBeklaAsync(ct);
            liste = await IdariYapiListesiCekAsync($"idariYapi/ilceListe/{ilId}", ct);
            _idCache.IlceListesiKaydet(ilId, liste);
        }

        return EslesenIdyiBul(liste, ilceAdi);
    }

    private async Task<int?> MahalleIdCozAsync(int ilceId, string mahalleAdi, CancellationToken ct)
    {
        var liste = _idCache.MahalleListesiGetir(ilceId);
        if (liste is null)
        {
            await _hizSinirlayici.BeklemeSuresinceBeklaAsync(ct);
            liste = await IdariYapiListesiCekAsync($"idariYapi/mahalleListe/{ilceId}", ct);
            _idCache.MahalleListesiKaydet(ilceId, liste);
        }

        return EslesenIdyiBul(liste, mahalleAdi);
    }

    /// <summary>
    /// Önce TAM normalize edilmiş eşleşme denenir. Bulunamazsa, boşluklar tamamen kaldırılarak
    /// TEKRAR denenir — gerçek bir WebTapu PDF'inde (2026-08-04 örneğiyle doğrulandı) uzun İlçe/
    /// Mahalle adları satır kaydırması yüzünden aralarına fazladan boşluk girmiş şekilde
    /// çıkabiliyor (ör. "Şehitkamil" -> "ŞEHİTKAMİ L"). Bu yedek adım, PDF'ten gelen böyle
    /// ufak boşluk farklarının TKGM eşleştirmesini bozmasını önler.
    /// </summary>
    private static int? EslesenIdyiBul(List<(int Id, string Text)> liste, string aranan)
    {
        var normalizeAranan = IlKodlari.Normallestir(aranan);

        foreach (var (id, text) in liste)
        {
            if (IlKodlari.Normallestir(text) == normalizeAranan) return id;
        }

        var bosluksuzAranan = normalizeAranan.Replace(" ", "");
        foreach (var (id, text) in liste)
        {
            if (IlKodlari.Normallestir(text).Replace(" ", "") == bosluksuzAranan) return id;
        }

        return null;
    }

    /// <summary>
    /// GET /idariYapi/{ilceListe|mahalleListe}/{id} — yanıt GeoJSON FeatureCollection'dır,
    /// her feature'ın properties.id (int) + properties.text (string) alanları kullanılır.
    /// Geometri (sınır poligonları) bilerek ATLANIR — yalnızca id/isim eşleştirmesi gerekli.
    /// </summary>
    private async Task<List<(int Id, string Text)>> IdariYapiListesiCekAsync(string yol, CancellationToken ct)
    {
        using var yanit = await _httpClient.GetAsync(yol, ct);
        yanit.EnsureSuccessStatusCode();

        var icerik = await yanit.Content.ReadAsStringAsync(ct);
        using var belge = JsonDocument.Parse(icerik);

        var sonuc = new List<(int, string)>();
        if (belge.RootElement.TryGetProperty("features", out var featuresEl))
        {
            foreach (var feature in featuresEl.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var props)) continue;
                if (!props.TryGetProperty("id", out var idEl)) continue;
                if (!props.TryGetProperty("text", out var textEl)) continue;

                sonuc.Add((idEl.GetInt32(), textEl.GetString() ?? string.Empty));
            }
        }

        return sonuc;
    }

    /// <summary>
    /// GET /parsel/{mahalleId}/{ada}/{parsel} — yanıt tek bir GeoJSON Feature'dır (geometry +
    /// properties: ilAd, ilceAd, mahalleAd, adaNo, parselNo, nitelik, alan, vb.). Burada yalnızca
    /// "parsel gerçekten var mı" (geometry alanı mevcut mu) doğrulanır; KML indirme adımı ayrıdır.
    /// </summary>
    private async Task<bool> ParselDogrulaAsync(int mahalleId, int ada, int parsel, CancellationToken ct)
    {
        await _hizSinirlayici.BeklemeSuresinceBeklaAsync(ct);

        using var yanit = await _httpClient.GetAsync($"parsel/{mahalleId}/{ada}/{parsel}", ct);
        if (!yanit.IsSuccessStatusCode) return false;

        var icerik = await yanit.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(icerik)) return false;

        try
        {
            using var belge = JsonDocument.Parse(icerik);
            return belge.RootElement.TryGetProperty("geometry", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<byte[]?> KmlIndirAsync(int mahalleId, int ada, int parsel, CancellationToken ct)
    {
        await _hizSinirlayici.BeklemeSuresinceBeklaAsync(ct);

        using var yanit = await _httpClient.GetAsync($"parsel/download/{mahalleId}/{ada}/{parsel}/kml", ct);
        if (!yanit.IsSuccessStatusCode) return null;

        return await yanit.Content.ReadAsByteArrayAsync(ct);
    }
}
