namespace ArsaTapu.DataAccess.Entities;

/// <summary>
/// Technical Defaults madde 5: Access Token kısa ömürlü, Refresh Token ile yenilenir.
/// Bu tablo Identity/Auth altyapısına aittir; Domain'deki iş varlıklarından ayrı tutulur.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime SonGecerlilikTarihi { get; set; }
    public bool Iptal { get; set; }
}
