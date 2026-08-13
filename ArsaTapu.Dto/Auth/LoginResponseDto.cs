namespace ArsaTapu.Dto.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenSonGecerlilik { get; set; }
    public string RefreshToken { get; set; } = null!;
    public string AdSoyad { get; set; } = null!;
    public IReadOnlyList<string> Roller { get; set; } = Array.Empty<string>();
}
