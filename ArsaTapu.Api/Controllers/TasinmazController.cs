using ArsaTapu.Api.Authorization;
using ArsaTapu.Business.Tasinmaz;
using ArsaTapu.Dto.Tasinmaz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

[Route("api/tasinmaz")]
public class TasinmazController : BaseApiController
{
    private readonly ITasinmazService _tasinmazService;

    public TasinmazController(ITasinmazService tasinmazService)
    {
        _tasinmazService = tasinmazService;
    }

    /// <summary>Herhangi bir rol çağırabilir; Patron için KisiId filtresi Business katmanında zorlanır.</summary>
    [HttpGet]
    public async Task<IActionResult> Listele([FromQuery] TasinmazFiltreDto filtre, CancellationToken ct)
    {
        var sonuc = await _tasinmazService.ListeleAsync(filtre, ct);
        return Basarili(sonuc);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Getir(int id, CancellationToken ct)
    {
        var sonuc = await _tasinmazService.GetirAsync(id, ct);
        return Basarili(sonuc);
    }

    [HttpPost]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Olustur([FromBody] TasinmazCreateDto istek, CancellationToken ct)
    {
        var sonuc = await _tasinmazService.OlusturAsync(istek, ct);
        return Olusturuldu(nameof(Getir), new { id = sonuc.Id }, sonuc);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Guncelle(int id, [FromBody] TasinmazUpdateDto istek, CancellationToken ct)
    {
        var sonuc = await _tasinmazService.GuncelleAsync(id, istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>
    /// Admin her kaydı silebilir; Personel yalnızca kendi yüklediği kayıtları silebilir
    /// (kontrol Business katmanında IYetkiKapsamService üzerinden yapılır, burada dağıtılmaz).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Sil(int id, CancellationToken ct)
    {
        await _tasinmazService.SilAsync(id, ct);
        return BasariliMesaj("Taşınmaz silindi.");
    }
}
