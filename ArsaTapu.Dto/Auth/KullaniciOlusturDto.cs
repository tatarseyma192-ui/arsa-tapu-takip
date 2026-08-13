namespace ArsaTapu.Dto.Auth;

/// <summary>
/// Yalnızca Admin kullanabilir (Requirements madde 1). Yeni bir giriş hesabı oluşturur ve
/// rolünü atar. Rol "Patron" ise KisiId ZORUNLUDUR — hesabı hangi Kişi'ye bağlayacağımızı
/// belirtir (Patron kendi profilini bu bağlantı üzerinden görür, bkz. IYetkiKapsamService).
/// </summary>
public class KullaniciOlusturDto
{
    public string Eposta { get; set; } = null!;
    public string Sifre { get; set; } = null!;
    public string AdSoyad { get; set; } = null!;

    /// <summary>"Admin" | "Personel" | "Patron"</summary>
    public string Rol { get; set; } = null!;

    /// <summary>Rol="Patron" iken zorunlu; diğer rollerde yok sayılır.</summary>
    public int? KisiId { get; set; }
}
