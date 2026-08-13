using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.Tekillestirme;

/// <summary>
/// Mülkiyet tekilleştirme anahtarı = BagimsizBolumNo + ZeminHisseId (kişi bazında).
/// TasinmazNo BİLEREK anahtarın DIŞINDA tutulur: 2026-08-04'te sağlanan gerçek bir Excel
/// örneğinde bu sütun hiç yoktu, ama aynı gerçek taşınmaz PDF'te (TasinmazNo'lu) VE Excel'de
/// (TasinmazNo'suz) görülebiliyor — TasinmazNo anahtara dahil edilseydi aynı taşınmaz iki
/// farklı kaynaktan yüklendiğinde YANLIŞLIKLA iki ayrı kayıt (iki kez "Yeni Alım") sanılırdı.
/// KML tekilleştirme anahtarından (IKmlTekillestirmeService) KASITLI olarak ayrı bir servistir.
/// </summary>
public interface IMulkiyetTekillestirmeService
{
    Task<bool> KayitliMiAsync(
        int kisiId, int? bagimsizBolumNo, string zeminHisseId, CancellationToken ct = default);

    /// <summary>
    /// Bir kişi için gelen aday listesini Yeni Alım / Zaten Kayıtlı olarak sınıflandırır.
    /// PDF/Excel parse motoru bu metodu kullanır.
    /// </summary>
    Task<MulkiyetTekillestirmeSonucuDto> SiniflandirAsync(
        int kisiId, IReadOnlyList<MulkiyetAdayDto> adaylar, CancellationToken ct = default);
}
