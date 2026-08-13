namespace ArsaTapu.Dto.Ortaklik;

/// <summary>
/// Komşuluk: aynı Ada/Parsel'de olup farklı Bağımsız Bölüm/Zemin Hisse ID'ye sahip
/// kişiler. Gerçek ortaklıkla karıştırılmamalıdır. Frontend'deki "Komşu mülk sahipleri"
/// tablosuna birebir denk gelir.
/// </summary>
public class KomsulukDto
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
    public List<KomsulukBirimDto> Birimler { get; set; } = new();
}
