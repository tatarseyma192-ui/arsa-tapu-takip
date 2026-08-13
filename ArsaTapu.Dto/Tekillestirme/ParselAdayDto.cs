namespace ArsaTapu.Dto.Tekillestirme;

/// <summary>
/// KML tekilleştirme anahtarı (mülkiyet anahtarından FARKLIDIR): Il + Ilce + Mahalle + Ada + Parsel.
/// </summary>
public class ParselAdayDto
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
}
