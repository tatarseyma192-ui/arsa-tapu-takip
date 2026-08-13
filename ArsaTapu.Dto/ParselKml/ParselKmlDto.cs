namespace ArsaTapu.Dto.ParselKml;

public class ParselKmlDto
{
    public int Id { get; set; }
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
    public string? DosyaYolu { get; set; }
    public DateTime CekilmeTarihi { get; set; }

    /// <summary>"Basarili" | "Basarisiz"</summary>
    public string Durum { get; set; } = null!;

    /// <summary>"Otomatik" | "Manuel"</summary>
    public string Kaynak { get; set; } = null!;

    /// <summary>
    /// Otomatik (TKGM) kayıtlar için true — TKGM entegrasyonu henüz gerçek sitede doğrulanmadı.
    /// Manuel yüklenen kayıtlar için false (birincil/güvenilir yol).
    /// </summary>
    public bool Deneysel { get; set; }

    /// <summary>Yalnızca Deneysel=true iken doludur.</summary>
    public string? DeneyselUyari { get; set; }
}
