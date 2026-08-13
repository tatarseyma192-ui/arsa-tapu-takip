using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Enums;

namespace ArsaTapu.Domain.Entities;

/// <summary>
/// Requirements madde 8: ParselKml.
/// KML tekilleştirme anahtarı (madde 4.2): Il + Ilce + Mahalle + Ada + Parsel.
/// Bu anahtar, Tasinmaz'ın mülkiyet tekilleştirme anahtarından KASITLI olarak farklıdır.
/// </summary>
public class ParselKml : BaseEntity
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }

    public string? DosyaYolu { get; set; }
    public DateTime CekilmeTarihi { get; set; }
    public KmlDurum Durum { get; set; }

    /// <summary>
    /// Otomatik (TKGM — şu an DENEYSEL) mi yoksa Manuel (kullanıcı yüklemesi — birincil/güvenilir) mi.
    /// TKGM entegrasyonu gerçek sitede bir süre sorunsuz doğrulanana kadar bu ayrım korunur.
    /// </summary>
    public ParselKmlKaynagi Kaynak { get; set; }
}
