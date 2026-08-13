namespace ArsaTapu.Dto.ParselKml;

public class ParselSorguSonucuDto
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }

    /// <summary>"Basarili" | "Basarisiz"</summary>
    public string Durum { get; set; } = null!;

    public string? DosyaYolu { get; set; }

    /// <summary>Yalnızca Durum=Basarisiz iken doludur; kullanıcıya gösterilebilir, teknik detay içermez.</summary>
    public string? HataMesaji { get; set; }

    /// <summary>
    /// Bu DTO yalnızca otomatik (TKGM) sorgu yolundan döner — bu yüzden HER ZAMAN true'dur.
    /// TKGM entegrasyonu gerçek sitede bir süre sorunsuz doğrulanana kadar bu işaret kaldırılmaz.
    /// </summary>
    public bool Deneysel { get; set; } = true;

    /// <summary>Ekranda her zaman gösterilmesi istenen uyarı metni (Basarili sonuçlarda da).</summary>
    public string DeneyselUyari { get; set; } = "Doğrulanmadı, kontrol edin.";
}
