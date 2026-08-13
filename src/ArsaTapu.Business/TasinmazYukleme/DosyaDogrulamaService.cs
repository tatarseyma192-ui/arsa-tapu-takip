using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace ArsaTapu.Business.TasinmazYukleme;

public class DosyaDogrulamaService : IDosyaDogrulamaService
{
    private readonly long _maksimumBoyutBayt;

    public DosyaDogrulamaService(IConfiguration configuration)
    {
        var maksimumMb = configuration.GetValue<int?>("DosyaYukleme:MaksimumBoyutMb") ?? 20;
        _maksimumBoyutBayt = (long)maksimumMb * 1024 * 1024;
    }

    public void Dogrula(string dosyaAdi, long boyutBayt, byte[] ilkBaytlar, KaynakTuru beklenenTur)
    {
        if (boyutBayt <= 0)
            throw new BusinessRuleException("Dosya boş görünüyor.");

        if (boyutBayt > _maksimumBoyutBayt)
            throw new BusinessRuleException(
                $"Dosya boyutu {_maksimumBoyutBayt / (1024 * 1024)} MB sınırını aşıyor.");

        var uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();

        switch (beklenenTur)
        {
            case KaynakTuru.Pdf:
                if (uzanti != ".pdf")
                    throw new BusinessRuleException("Yalnızca .pdf uzantılı dosyalar kabul edilir.");
                if (!ImzaEslesiyorMu(ilkBaytlar, "%PDF"u8.ToArray()))
                    throw new BusinessRuleException(
                        "Dosya içeriği geçerli bir PDF gibi görünmüyor (dosya bozuk veya uzantısı yanlış olabilir).");
                break;

            case KaynakTuru.Excel:
                if (uzanti != ".xlsx")
                    throw new BusinessRuleException(
                        "Yalnızca .xlsx uzantılı Excel dosyaları kabul edilir (eski .xls formatı desteklenmiyor).");
                if (!ImzaEslesiyorMu(ilkBaytlar, new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                    throw new BusinessRuleException(
                        "Dosya içeriği geçerli bir Excel (.xlsx) dosyası gibi görünmüyor.");
                break;

            default:
                throw new BusinessRuleException("Desteklenmeyen kaynak türü.");
        }
    }

    private static bool ImzaEslesiyorMu(byte[] ilkBaytlar, byte[] beklenenImza)
    {
        if (ilkBaytlar.Length < beklenenImza.Length) return false;
        for (var i = 0; i < beklenenImza.Length; i++)
        {
            if (ilkBaytlar[i] != beklenenImza[i]) return false;
        }
        return true;
    }

    public void KmlDogrula(string dosyaAdi, long boyutBayt)
    {
        if (boyutBayt <= 0)
            throw new BusinessRuleException("Dosya boş görünüyor.");

        // KML dosyaları küçüktür; taşınmaz/Excel yükleme sınırından çok daha düşük bir tavan yeterli.
        const long kmlMaksimumBoyutBayt = 5 * 1024 * 1024;
        if (boyutBayt > kmlMaksimumBoyutBayt)
            throw new BusinessRuleException("KML dosyası 5 MB sınırını aşıyor.");

        var uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();
        if (uzanti != ".kml")
            throw new BusinessRuleException("Yalnızca .kml uzantılı dosyalar kabul edilir.");
    }
}
