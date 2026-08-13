namespace ArsaTapu.Dto.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Kayitlar { get; set; } = Array.Empty<T>();
    public int ToplamKayit { get; set; }
    public int Sayfa { get; set; }
    public int SayfaBoyutu { get; set; }
    public int ToplamSayfa => SayfaBoyutu == 0 ? 0 : (int)Math.Ceiling(ToplamKayit / (double)SayfaBoyutu);
}
