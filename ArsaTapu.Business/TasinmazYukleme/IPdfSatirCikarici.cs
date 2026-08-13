namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Requirements madde 2.1: WebTapu/e-Devlet PDF çıktısından (çok sayfalı olabilir) tabloyu
/// otomatik çıkarır, tüm sayfaları tek tabloda birleştirir. PDF yapısı zamanla değişebileceği
/// için bu arayüzün ARKASINDAKİ implementasyon izole tutulur (Requirements madde 5) — sütun
/// şeması/karşılaştırma mantığı bundan ETKİLENMEZ, yalnızca bu sınıf güncellenir.
/// </summary>
public interface IPdfSatirCikarici
{
    /// <summary>Ham satırları (kanonik sütun adı -> hücre metni) tüm sayfalar birleştirilmiş olarak döner.</summary>
    List<Dictionary<string, string?>> SatirlariCikar(Stream pdfStream);
}
