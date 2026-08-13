namespace ArsaTapu.Dto.Tekillestirme;

/// <summary>
/// PDF/Excel parse motorunun üreteceği aday satır.
/// Mülkiyet tekilleştirme anahtarı: BagimsizBolumNo + ZeminHisseId. TasinmazNo BİLEREK
/// nullable — 2026-08-04'te sağlanan gerçek bir Excel örneğinde bu sütun hiç yoktu; yalnızca
/// varsa görüntüleme/referans amaçlı taşınır, eşleştirmede kullanılmaz.
/// </summary>
public class MulkiyetAdayDto
{
    public string? TasinmazNo { get; set; }
    public int? BagimsizBolumNo { get; set; }
    public string ZeminHisseId { get; set; } = null!;

    public string? Nitelik { get; set; }
    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public string? Mahalle { get; set; }
    public int? Ada { get; set; }
    public int? Parsel { get; set; }
    public decimal? Yuzolcum { get; set; }
}
