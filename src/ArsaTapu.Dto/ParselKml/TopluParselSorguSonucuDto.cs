namespace ArsaTapu.Dto.ParselKml;

public class TopluParselSorguSonucuDto
{
    public int ToplamSorgu { get; set; }
    public int BasariliSayisi { get; set; }
    public int BasarisizSayisi { get; set; }

    /// <summary>Bu istekten önce zaten başarıyla çekilmiş olduğu için sorgulanmayan parsel sayısı.</summary>
    public int AtlananSayisi { get; set; }

    public List<ParselSorguSonucuDto> Sonuclar { get; set; } = new();
}
