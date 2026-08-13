namespace ArsaTapu.Business.TasinmazYukleme.Pdf;

/// <summary>
/// Bir PDF kelimesinin metni + konumu. PdfPig'e (veya başka bir PDF kütüphanesine) bağımlı
/// DEĞİLDİR — bu, TabloSatirOlusturucu'nun saf/test edilebilir kalmasını sağlar. PDF kütüphanesi
/// değişirse yalnızca IPdfSatirCikarici implementasyonu bu tipi üretecek şekilde güncellenir.
/// </summary>
public sealed record KonumluKelime(string Metin, double SolX, double SagX, double UstY, double AltY)
{
    public double MerkezX => (SolX + SagX) / 2;
    public double MerkezY => (UstY + AltY) / 2;
    public double Yukseklik => Math.Abs(UstY - AltY);
}
