using ArsaTapu.Business.Ortaklik;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

/// <summary>
/// Frontend'deki "Gerçek ortaklık" / "Komşu mülk sahipleri" tablolarına birebir denk gelir.
/// Hesaplama tamamen Business katmanında (OrtaklikService) yapılır.
/// </summary>
[Route("api/ortaklik")]
public class OrtaklikController : BaseApiController
{
    private readonly IOrtaklikService _ortaklikService;

    public OrtaklikController(IOrtaklikService ortaklikService)
    {
        _ortaklikService = ortaklikService;
    }

    /// <summary>Gerçek ortaklık: aynı Bağımsız Bölüm No + aynı Zemin Hisse ID.</summary>
    [HttpGet("gercek")]
    public async Task<IActionResult> GercekOrtaklik([FromQuery] int[]? kisiIds, CancellationToken ct)
    {
        var sonuc = await _ortaklikService.GercekOrtaklikGetirAsync(kisiIds, ct);
        return Basarili(sonuc);
    }

    /// <summary>Komşuluk: aynı Ada/Parsel, farklı Bağımsız Bölüm — ortaklık değildir.</summary>
    [HttpGet("komsuluk")]
    public async Task<IActionResult> Komsuluk([FromQuery] int[]? kisiIds, CancellationToken ct)
    {
        var sonuc = await _ortaklikService.KomsulukGetirAsync(kisiIds, ct);
        return Basarili(sonuc);
    }
}
