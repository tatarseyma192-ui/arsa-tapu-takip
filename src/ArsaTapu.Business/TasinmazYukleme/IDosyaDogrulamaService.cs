using ArsaTapu.Domain.Enums;

namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Dosya yüklemelerinin güvenli şekilde doğrulanması (Handbook madde 9). Uzantı, boyut ve
/// "magic byte" (dosya imzası) kontrolü yapar; kullanıcının dosyayı yanlışlıkla yeniden
/// adlandırması veya bozuk/sahte bir dosya göndermesi durumunda parser'a ulaşmadan net bir
/// hata ile durur.
/// </summary>
public interface IDosyaDogrulamaService
{
    /// <summary>Doğrulama başarısızsa BusinessRuleException fırlatır (kullanıcıya gösterilebilir mesajla).</summary>
    void Dogrula(string dosyaAdi, long boyutBayt, byte[] ilkBaytlar, KaynakTuru beklenenTur);

    /// <summary>Manuel KML yüklemesi için (Requirements madde 5 — "kendi indirdiği KML'i manuel yükleyebilir").</summary>
    void KmlDogrula(string dosyaAdi, long boyutBayt);
}
