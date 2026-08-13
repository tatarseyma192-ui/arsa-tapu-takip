using ArsaTapu.Dto.Kisi;

namespace ArsaTapu.Dto.Ortaklik;

/// <summary>Bir ada/parsel içindeki tek bir bağımsız bölüm/hisse birimi ve sahipleri.</summary>
public class KomsulukBirimDto
{
    public int? BagimsizBolumNo { get; set; }
    public string ZeminHisseId { get; set; } = null!;
    public List<KisiKisaDto> Kisiler { get; set; } = new();
}
