namespace ArsaTapu.Dto.Auth;

public class LoginRequestDto
{
    public string KullaniciAdiVeyaEposta { get; set; } = null!;
    public string Sifre { get; set; } = null!;
}
