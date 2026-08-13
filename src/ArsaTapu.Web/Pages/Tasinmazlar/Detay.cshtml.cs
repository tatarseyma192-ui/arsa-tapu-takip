using ArsaTapu.Dto.Tasinmaz;
using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArsaTapu.Web.Pages.Tasinmazlar;

[Authorize]
public class DetayModel : PageModel
{
    private readonly IApiIstemcisi _api;

    public DetayModel(IApiIstemcisi api)
    {
        _api = api;
    }

    public TasinmazDto? Tasinmaz { get; private set; }
    public string? HataMesaji { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var jwt = User.FindFirst("jwt")?.Value;
        if (jwt is null) return RedirectToPage("/Giris");

        Tasinmaz = await _api.TasinmazDetayGetirAsync(id, jwt);
        if (Tasinmaz is null)
            HataMesaji = "Taşınmaz bulunamadı.";

        return Page();
    }
}
