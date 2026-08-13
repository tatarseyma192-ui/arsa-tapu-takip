using ArsaTapu.Dto.Common;

namespace ArsaTapu.Dto.Tasinmaz;

public class TasinmazFiltreDto : PagedRequest
{
    public int? KisiId { get; set; }

    /// <summary>"Aktif" | "Satildi"</summary>
    public string? Durum { get; set; }

    public string? Il { get; set; }
    public string? Ilce { get; set; }
    public string? Mahalle { get; set; }
    public int? Ada { get; set; }
    public int? Parsel { get; set; }
}
