using System.Security.Claims;
using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArsaTapu.Web.Pages;

public class GirisModel : PageModel
{
    private readonly IApiIstemcisi _api;

    public GirisModel(IApiIstemcisi api)
    {
        _api = api;
    }

    [BindProperty]
    public string KullaniciAdiVeyaEposta { get; set; } = "";

    [BindProperty]
    public string Sifre { get; set; } = "";

    public string? HataMesaji { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var girisSonucu = await _api.GirisYapAsync(KullaniciAdiVeyaEposta, Sifre);

        if (girisSonucu is null)
        {
            HataMesaji = "E-posta veya şifre hatalı.";
            return Page();
        }

        // JWT burada, kullanıcının kendi tarayıcı çerezinin İÇİNDE saklanır (BFF deseni) —
        // her sayfa isteğinde tekrar API'ye giriş yapmasına gerek kalmaz. ApiIstemcisi'nin
        // sonraki her çağrısı bu claim'i okuyup Authorization: Bearer header'ına koyar.
        var claimler = new List<Claim>
        {
            new(ClaimTypes.Name, girisSonucu.AdSoyad),
            new("jwt", girisSonucu.AccessToken)
        };
        claimler.AddRange(girisSonucu.Roller.Select(r => new Claim(ClaimTypes.Role, r)));

        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));

        return RedirectToPage("/Kisiler/Index");
    }
}
