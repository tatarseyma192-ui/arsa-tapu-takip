using ArsaTapu.Dto.Kisi;

namespace ArsaTapu.Dto.Ortaklik;

/// <summary>
/// Gerçek ortaklık (hisseli mülkiyet): aynı Bağımsız Bölüm No + aynı Zemin Hisse ID.
/// Frontend'deki "Gerçek ortaklık" tablosuna birebir denk gelir.
/// </summary>
public class GercekOrtaklikDto
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
    public int? BagimsizBolumNo { get; set; }
    public string ZeminHisseId { get; set; } = null!;
    public List<KisiKisaDto> OrtakKisiler { get; set; } = new();
}
