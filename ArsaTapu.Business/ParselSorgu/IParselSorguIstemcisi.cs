namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// Requirements madde 5: TKGM (https://parselsorgu.tkgm.gov.tr/) ile konuşan TEK arayüz.
/// Handbook madde 4: "Hiçbir entegrasyon uygulamanın temel mimarisine bağımlı hale
/// getirilmemelidir... Servis değişiklikleri minimum kod değişikliği ile yapılabilmelidir."
/// TKGM site yapısı değişirse (veya farklı bir kaynağa geçilirse) yalnızca bu arayüzün
/// implementasyonu (TkgmParselSorguIstemcisi) değişir; ParselKmlService, rate limiter,
/// dosya deposu ve KML oluşturucu ETKİLENMEZ.
/// </summary>
public interface IParselSorguIstemcisi
{
    Task<ParselSorguIstemciSonucu> SorgulaAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default);
}
