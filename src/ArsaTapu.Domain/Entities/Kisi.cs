using ArsaTapu.Domain.Common;

namespace ArsaTapu.Domain.Entities;

/// <summary>
/// Requirements madde 8: Kisi (Id, AdSoyad, KullaniciId nullable).
/// </summary>
public class Kisi : BaseEntity
{
    public string AdSoyad { get; set; } = null!;

    /// <summary>ASP.NET Identity kullanıcı Id'si. Patron girişi varsa doldurulur.</summary>
    public string? KullaniciId { get; set; }

    public ICollection<YuklemeKaydi> YuklemeKayitlari { get; set; } = new List<YuklemeKaydi>();
    public ICollection<Tasinmaz> Tasinmazlar { get; set; } = new List<Tasinmaz>();
}
