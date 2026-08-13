using ArsaTapu.Api.Authorization;
using ArsaTapu.Business.Kisi;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Kisi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

[Route("api/kisi")]
public class KisiController : BaseApiController
{
    private readonly IKisiService _kisiService;

    public KisiController(IKisiService kisiService)
    {
        _kisiService = kisiService;
    }

    /// <summary>Tüm kişileri listeler (sayfalı, aranabilir). Patron bu uca erişemez.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Listele([FromQuery] PagedRequest istek, CancellationToken ct)
    {
        var sonuc = await _kisiService.ListeleAsync(istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>Patron'un kendi profilini görüntülemesi için kısayol.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> KendiProfilim(CancellationToken ct)
    {
        var sonuc = await _kisiService.KendiProfiliniGetirAsync(ct);
        if (sonuc is null)
            return Hatali("Bu kullanıcıya bağlı bir kişi profili bulunamadı.", StatusCodes.Status404NotFound);

        return Basarili(sonuc);
    }

    /// <summary>Herhangi bir rol erişebilir; Patron için kapsam Business katmanında zorlanır.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Getir(int id, CancellationToken ct)
    {
        var sonuc = await _kisiService.GetirAsync(id, ct);
        return Basarili(sonuc);
    }

    [HttpPost]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Olustur([FromBody] KisiCreateDto istek, CancellationToken ct)
    {
        var sonuc = await _kisiService.OlusturAsync(istek, ct);
        return Olusturuldu(nameof(Getir), new { id = sonuc.Id }, sonuc);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PolicyIsimleri.YonetimVePersonel)]
    public async Task<IActionResult> Guncelle(int id, [FromBody] KisiUpdateDto istek, CancellationToken ct)
    {
        var sonuc = await _kisiService.GuncelleAsync(id, istek, ct);
        return Basarili(sonuc);
    }

    /// <summary>Yalnızca Admin silebilir (Kişi/patron kaydı silmek en hassas işlemdir).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PolicyIsimleri.SadeceYonetim)]
    public async Task<IActionResult> Sil(int id, CancellationToken ct)
    {
        await _kisiService.SilAsync(id, ct);
        return BasariliMesaj("Kişi silindi.");
    }
}
