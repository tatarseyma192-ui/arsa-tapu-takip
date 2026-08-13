using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Enums;

namespace ArsaTapu.Domain.Entities;

/// <summary>
/// Requirements madde 8: YuklemeKaydi (Id, KisiId, YuklemeTarihi, KaynakDosyaAdi,
/// KaynakTuru, YukleyenKullaniciId).
/// </summary>
public class YuklemeKaydi : BaseEntity
{
    public int KisiId { get; set; }
    public Kisi? Kisi { get; set; }

    public DateTime YuklemeTarihi { get; set; }
    public string KaynakDosyaAdi { get; set; } = null!;
    public KaynakTuru KaynakTuru { get; set; }

    /// <summary>Bu yüklemeyi yapan kullanıcı Id'si (Personel silme yetkisi kapsamı için kullanılır).</summary>
    public string YukleyenKullaniciId { get; set; } = null!;
}
