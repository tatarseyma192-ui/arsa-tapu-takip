namespace ArsaTapu.DataAccess.Repositories;

public interface ITasinmazRepository : IRepository<Domain.Entities.Tasinmaz>
{
    /// <summary>
    /// Bir kişiye ait mevcut mülkiyet tekilleştirme anahtarlarını (BagimsizBolumNo, ZeminHisseId)
    /// getirir. TasinmazNo BİLEREK bu anahtara DAHİL DEĞİL (bkz. Tasinmaz.cs — bazı kaynaklarda,
    /// ör. Excel'de, hiç bulunmayabiliyor). Tekilleştirme mantığı Business katmanında
    /// (IMulkiyetTekillestirmeService) bu anahtarlar üzerinden yürütülür.
    /// </summary>
    Task<List<(int? BagimsizBolumNo, string ZeminHisseId)>> MevcutAnahtarlariGetirAsync(
        int kisiId, CancellationToken ct = default);
}
