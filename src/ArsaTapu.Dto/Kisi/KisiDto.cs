namespace ArsaTapu.Dto.Kisi;

public class KisiDto
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = null!;
    public string? KullaniciId { get; set; }
    public int AktifTasinmazSayisi { get; set; }
    public int SatilanTasinmazSayisi { get; set; }
    public int YuklemeSayisi { get; set; }
}
