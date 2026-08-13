using ArsaTapu.Dto.Kisi;
using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArsaTapu.Web.Pages.Kisiler;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IApiIstemcisi _api;

    public IndexModel(IApiIstemcisi api)
    {
        _api = api;
    }

    public IReadOnlyList<KisiDto> Kisiler { get; private set; } = Array.Empty<KisiDto>();
    public string? HataMesaji { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Arama { get; set; }

    public async Task OnGetAsync()
    {
        var jwt = User.FindFirst("jwt")?.Value;
        if (jwt is null) return;

        var sonuc = await _api.KisileriListeleAsync(jwt, arama: Arama);
        if (sonuc is null)
        {
            HataMesaji = "Kişi listesi alınamadı — API'ye ulaşılamıyor olabilir.";
            return;
        }

        Kisiler = sonuc.Kayitlar.ToList();
    }
}
