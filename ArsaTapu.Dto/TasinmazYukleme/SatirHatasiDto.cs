namespace ArsaTapu.Dto.TasinmazYukleme;

/// <summary>Bir satırın parse/doğrulama sırasında atlanma sebebi (teknik detay değil, kullanıcıya gösterilebilir).</summary>
public class SatirHatasiDto
{
    public int SatirNo { get; set; }
    public string Mesaj { get; set; } = null!;
}
