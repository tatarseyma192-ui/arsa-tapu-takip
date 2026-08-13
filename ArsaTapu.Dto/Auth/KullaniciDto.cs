namespace ArsaTapu.Dto.Auth;

public class KullaniciDto
{
    public string Id { get; set; } = null!;
    public string Eposta { get; set; } = null!;
    public string? AdSoyad { get; set; }

    /// <summary>Bir kullanıcının teorik olarak birden fazla rolü olabilir; pratikte tek rol beklenir.</summary>
    public List<string> Roller { get; set; } = new();

    /// <summary>Yalnızca Patron rolündeki kullanıcılarda dolu olur.</summary>
    public int? KisiId { get; set; }
    public string? KisiAdSoyad { get; set; }
}
