namespace ArsaTapu.DataAccess.Repositories;

public interface IParselKmlRepository : IRepository<Domain.Entities.ParselKml>
{
    /// <summary>
    /// Başarıyla KML'si çekilmiş Il/Ilce/Mahalle/Ada/Parsel anahtarlarını getirir.
    /// KML tekilleştirme mantığı (IKmlTekillestirmeService) bu anahtarlar üzerinden yürütülür.
    /// </summary>
    Task<List<(string Il, string Ilce, string Mahalle, int Ada, int Parsel)>> BasariliAnahtarlariGetirAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Requirements madde 4.3 (yeniden sorgulanabilme) + retry akışı için: verilen anahtarla
    /// eşleşen MEVCUT (silinmemiş) kaydı bulur — varsa üzerine güncellenir, yoksa yeni oluşturulur
    /// (unique index çakışmasını önlemek için ParselKmlService bunu her zaman önce kontrol eder).
    /// </summary>
    Task<Domain.Entities.ParselKml?> AnahtarIleBulAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default);
}
