using Microsoft.AspNetCore.Http;

namespace ArsaTapu.Api.Controllers;

/// <summary>multipart/form-data ile gelen dosya + hedef Kişi Id'si (IFormFile bu yüzden Dto katmanında değil, Api'de tanımlı).</summary>
public class DosyaYuklemeIstegi
{
    public IFormFile Dosya { get; set; } = null!;
    public int KisiId { get; set; }
}
