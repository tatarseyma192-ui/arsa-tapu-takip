using ArsaTapu.Dto.Kisi;
using ArsaTapu.Dto.Tasinmaz;
using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArsaTapu.Web.Pages.Kisiler;

[Authorize]
public class DetayModel : PageModel
{
    private readonly IApiIstemcisi _api;

    public DetayModel(IApiIstemcisi api)
    {
        _api = api;
    }

    public KisiDto? Kisi { get; private set; }
    public IReadOnlyList<TasinmazDto> Portfoy { get; private set; } = Array.Empty<TasinmazDto>();
    public string? HataMesaji { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var jwt = User.FindFirst("jwt")?.Value;
        if (jwt is null) return RedirectToPage("/Giris");

        Kisi = await _api.KisiGetirAsync(id, jwt);
        if (Kisi is null)
        {
            HataMesaji = "Kişi bulunamadı.";
            return Page();
        }

        var tasinmazlar = await _api.KisininTasinmazlariniGetirAsync(id, jwt);
        Portfoy = tasinmazlar?.Kayitlar.ToList() ?? new List<TasinmazDto>();

        return Page();
    }
}
