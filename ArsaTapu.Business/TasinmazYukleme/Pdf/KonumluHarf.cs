namespace ArsaTapu.Business.TasinmazYukleme.Pdf;

/// <summary>
/// Tek bir PDF karakterinin (glyph) metni + konumu. Gerçek bir WebTapu PDF'inde (2026-08-04'te
/// sağlanan örnekle doğrulandı) büyük, döndürülmüş bir filigran ("BİLGİ AMAÇLIDIR", her harfi
/// 100+ punto) gerçek 12pt tablo metniyle AYNI KELİMEYE karışabiliyor (ör. "241.51Ç1093" gibi
/// Yüzölçüm+filigran+Ada birleşmesi) — bu, kelime seviyesinde (PdfPig'in GetWords()'u) filtrelemeyle
/// ÇÖZÜLEMEZ, çünkü birleşme zaten kelime oluşturulurken gerçekleşir. Bu yüzden filigran, KARAKTER
/// seviyesinde (sınırlayıcı kutu yüksekliğine göre) filtrelenip kelimeler sıfırdan bu temiz
/// karakterlerden yeniden kurulur (bkz. TabloSatirOlusturucu.HarflerdenKelimeOlustur).
/// </summary>
public sealed record KonumluHarf(string Metin, double SolX, double SagX, double UstY, double AltY)
{
    public double Yukseklik => Math.Abs(UstY - AltY);
}
