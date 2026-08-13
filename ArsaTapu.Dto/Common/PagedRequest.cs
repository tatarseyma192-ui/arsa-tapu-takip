namespace ArsaTapu.Dto.Common;

public class PagedRequest
{
    private int _sayfa = 1;
    private int _sayfaBoyutu = 25;

    public int Sayfa
    {
        get => _sayfa;
        set => _sayfa = value < 1 ? 1 : value;
    }

    public int SayfaBoyutu
    {
        get => _sayfaBoyutu;
        set => _sayfaBoyutu = value is < 1 or > 200 ? 25 : value;
    }

    public string? Arama { get; set; }
}
