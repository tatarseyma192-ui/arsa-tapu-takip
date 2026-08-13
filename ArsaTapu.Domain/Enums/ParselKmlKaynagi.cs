namespace ArsaTapu.Domain.Enums;

/// <summary>
/// Bir ParselKml kaydının nasıl elde edildiği. Otomatik (TKGM) yol şu an DENEYSEL kabul edilir
/// (gerçek TKGM sitesine karşı doğrulanmadı); Manuel yol birincil/güvenilir yoldur — kullanıcı
/// kendi indirdiği dosyayı yükler.
/// </summary>
public enum ParselKmlKaynagi
{
    Otomatik = 1,
    Manuel = 2
}
