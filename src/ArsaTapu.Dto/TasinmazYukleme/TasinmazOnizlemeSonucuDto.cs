namespace ArsaTapu.Dto.TasinmazYukleme;

/// <summary>
/// Önizleme uç noktalarının (PDF/Excel) yanıtı. Bu aşamada HİÇBİR veritabanı yazımı yapılmaz
/// (Requirements madde 2.1) — kullanıcı bu listeyi kontrol edip /onayla ucuna aynı satırları gönderir.
/// </summary>
public class TasinmazOnizlemeSonucuDto
{
    public string KaynakDosyaAdi { get; set; } = null!;

    /// <summary>"Pdf" | "Excel"</summary>
    public string KaynakTuru { get; set; } = null!;

    public int ToplamSatirSayisi { get; set; }
    public int GecerliSatirSayisi { get; set; }
    public int YeniAlimSayisi { get; set; }
    public int ZatenKayitliSayisi { get; set; }

    public List<TasinmazOnizlemeSatiriDto> Satirlar { get; set; } = new();
    public List<SatirHatasiDto> SatirHatalari { get; set; } = new();
}
