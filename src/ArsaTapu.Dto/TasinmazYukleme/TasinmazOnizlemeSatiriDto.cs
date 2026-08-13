using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Dto.TasinmazYukleme;

/// <summary>
/// Önizleme ekranındaki tek satır: ham aday veri + tekilleştirme sınıflandırması.
/// "YeniAlim" | "ZatenKayitli" (Requirements madde 4.1).
/// </summary>
public class TasinmazOnizlemeSatiriDto
{
    public int SatirNo { get; set; }
    public MulkiyetAdayDto Aday { get; set; } = null!;
    public string Durum { get; set; } = null!;
}
