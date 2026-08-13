using ArsaTapu.Business.TasinmazYukleme.Pdf;
using ArsaTapu.Domain.Exceptions;
using UglyToad.PdfPig;

namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Requirements madde 2.1 + madde 5: WebTapu/e-Devlet PDF çıktısını okur, tüm sayfaları
/// birleştirir. PdfPig'e BAĞIMLI TEK sınıf budur — kütüphane veya PDF yapısı değişirse
/// yalnızca bu dosya güncellenir; TabloSatirOlusturucu (saf algoritma) ve SatirDonusturucu
/// (satır -> DTO dönüşümü) ETKİLENMEZ.
///
/// 2026-08-04'te sağlanan GERÇEK bir WebTapu PDF örneğiyle doğrulandı (30/30 taşınmaz kaydı
/// doğru çıkarıldı). Bu doğrulama sürecinde iki gerçek sorun bulunup çözüldü:
///   1. Büyük, döndürülmüş bir filigran ("BİLGİ AMAÇLIDIR", ~100+ punto) gerçek 12pt tablo
///      metniyle karışabiliyordu — PdfPig'in kelime düzeyi GetWords()'u yerine KARAKTER
///      düzeyinde (Letters) okuyup yazı boyutuna göre filigranı ayıklıyoruz, sonra kelimeleri
///      sıfırdan kuruyoruz (bkz. TabloSatirOlusturucu.HarflerdenKelimeOlustur).
///   2. Uzun Nitelik/İlçe/Mahalle değerleri birden fazla görsel satıra yayılabiliyor — bu
///      "devam parçaları" bir önceki gerçek satıra birleştiriliyor (bkz.
///      TabloSatirOlusturucu.SatirParcalariniBirlestir).
/// </summary>
public class PdfSatirCikarici : IPdfSatirCikarici
{
    private static readonly int BeklenenSutunSayisi = KanonikSutunlar.Tumu.Length;

    private static readonly int[] AnahtarSutunIndeksleri =
    {
        Array.IndexOf(KanonikSutunlar.Tumu, KanonikSutunlar.TasinmazNo),
        Array.IndexOf(KanonikSutunlar.Tumu, KanonikSutunlar.Ada),
        Array.IndexOf(KanonikSutunlar.Tumu, KanonikSutunlar.Parsel),
        Array.IndexOf(KanonikSutunlar.Tumu, KanonikSutunlar.ZeminHisseId)
    };

    public List<Dictionary<string, string?>> SatirlariCikar(Stream pdfStream)
    {
        List<List<KonumluKelime>> tumSatirlar;

        try
        {
            tumSatirlar = SayfalariOkuVeSatirlaraAyir(pdfStream);
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            throw new BusinessRuleException(
                "PDF dosyası okunamadı. Dosyanın bozuk olmadığından ve WebTapu/e-Devlet çıktısı olduğundan emin olun.");
        }

        if (tumSatirlar.Count == 0)
        {
            throw new BusinessRuleException(
                "PDF içinde okunabilir metin bulunamadı. Dosya taranmış bir görüntü (resim) olabilir; " +
                "bu durumda sistem yalnızca metin tabanlı (WebTapu/e-Devlet çıktısı) PDF'leri işleyebilir.");
        }

        var baslikIndex = TabloSatirOlusturucu.BaslikSatiriniBul(tumSatirlar);
        if (baslikIndex is null)
        {
            throw new BusinessRuleException(
                $"PDF içinde beklenen sütun başlıkları bulunamadı. Sütunlar şu şekilde olmalı: " +
                $"{KanonikSutunlar.BeklenenBasliklarMetni()}");
        }

        var sutunSinirlari = TabloSatirOlusturucu.SutunSinirlariniHesapla(tumSatirlar[baslikIndex.Value], BeklenenSutunSayisi);
        if (sutunSinirlari.Count != BeklenenSutunSayisi)
        {
            throw new BusinessRuleException(
                $"PDF'teki sütun sayısı beklenenden farklı (bulunan: {sutunSinirlari.Count}, beklenen: {BeklenenSutunSayisi}). " +
                "PDF formatı değişmiş olabilir.");
        }

        // Birleştirme eşiği: normal satır-arası boşluğun biraz altı/üstünü kapsayacak ama
        // sayfa altbilgisi gibi belirgin büyük boşlukları dışarıda bırakacak şekilde,
        // sayfadaki ortalama kelime yüksekliğinden türetilir (gerçek örnekte doğrulandı:
        // devam parçası boşluğu ~14pt, normal satır boşluğu ~19pt, altbilgi boşluğu ~29pt).
        var tumKelimeler = tumSatirlar.SelectMany(s => s).ToList();
        var ortalamaYukseklik = tumKelimeler.Count > 0 ? tumKelimeler.Average(k => k.Yukseklik) : 12.0;
        var birlestirmeEsigiY = ortalamaYukseklik * 2.0;

        var satirBilgileri = new List<(double OrtalamaY, string[] Sutunlar)>();

        for (var i = 0; i < tumSatirlar.Count; i++)
        {
            if (i == baslikIndex.Value) continue; // başlık satırı veri olarak işlenmez

            var sutunMetinleri = TabloSatirOlusturucu.SatiriSutunlaraAyir(tumSatirlar[i], sutunSinirlari);
            if (sutunMetinleri.All(string.IsNullOrWhiteSpace)) continue; // tamamen boş satır

            var ortalamaY = tumSatirlar[i].Average(k => k.MerkezY);
            satirBilgileri.Add((ortalamaY, sutunMetinleri));
        }

        var birlesmisSatirlar = TabloSatirOlusturucu.SatirParcalariniBirlestir(
            satirBilgileri, AnahtarSutunIndeksleri, birlestirmeEsigiY);

        var sonuc = new List<Dictionary<string, string?>>();
        foreach (var sutunMetinleri in birlesmisSatirlar)
        {
            var satirVerisi = new Dictionary<string, string?>();
            for (var s = 0; s < KanonikSutunlar.Tumu.Length; s++)
                satirVerisi[KanonikSutunlar.Tumu[s]] = sutunMetinleri[s];

            sonuc.Add(satirVerisi);
        }

        return sonuc;
    }

    private static List<List<KonumluKelime>> SayfalariOkuVeSatirlaraAyir(Stream pdfStream)
    {
        var tumSatirlar = new List<List<KonumluKelime>>();

        using var belge = PdfDocument.Open(pdfStream);

        foreach (var sayfa in belge.GetPages())
        {
            var sayfaHarfleri = sayfa.Letters
                .Where(h => !string.IsNullOrWhiteSpace(h.Value))
                .Select(h => new KonumluHarf(
                    h.Value,
                    h.GlyphRectangle.Left,
                    h.GlyphRectangle.Right,
                    h.GlyphRectangle.Top,
                    h.GlyphRectangle.Bottom))
                .ToList();

            if (sayfaHarfleri.Count == 0) continue;

            var sayfaKelimeleri = TabloSatirOlusturucu.HarflerdenKelimeOlustur(sayfaHarfleri);
            if (sayfaKelimeleri.Count == 0) continue;

            // ÖNEMLİ: satır kümeleme HER SAYFA İÇİN AYRI yapılır (farklı sayfalardaki
            // kelimeler aynı Y aralığına düşebilir; sayfalar arası yanlış birleşmeyi önler),
            // sonra sayfa sırasına göre tek listede birleştirilir (Requirements madde 2:
            // "tüm sayfalar otomatik birleştirilerek tek tabloya dönüştürülür").
            var sayfaSatirlari = TabloSatirOlusturucu.SatirlaraGrupla(sayfaKelimeleri);
            tumSatirlar.AddRange(sayfaSatirlari);
        }

        return tumSatirlar;
    }
}
