using ArsaTapu.Dto.Kisi;
using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authorization;
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

    public async Task OnGetAsync(string? arama)
    {
        var jwt = User.FindFirst("jwt")?.Value;
        if (jwt is null) return;

        var sonuc = await _api.KisileriListeleAsync(jwt, arama: arama);
        if (sonuc is null)
        {
            HataMesaji = "Kişi listesi alınamadı — API'ye ulaşılamıyor olabilir.";
            return;
        }

        Kisiler = sonuc.Kayitlar.ToList();
    }
}
