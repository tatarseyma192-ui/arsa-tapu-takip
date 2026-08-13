using Microsoft.AspNetCore.Http;

namespace ArsaTapu.Api.Controllers;

/// <summary>Kullanıcının kendi indirdiği KML dosyasını manuel yüklemesi için istek modeli.</summary>
public class ParselKmlManuelYuklemeIstegi
{
    public IFormFile Dosya { get; set; } = null!;
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
}
