using ArsaTapu.Api.Services;
using ArsaTapu.DataAccess.Identity;
using ArsaTapu.Dto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArsaTapu.Api.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto istek)
    {
        var kullanici = await _userManager.FindByNameAsync(istek.KullaniciAdiVeyaEposta)
                        ?? await _userManager.FindByEmailAsync(istek.KullaniciAdiVeyaEposta);

        if (kullanici is null)
            return Hatali("Kullanıcı adı/e-posta veya şifre hatalı.", StatusCodes.Status401Unauthorized);

        var sonuc = await _signInManager.CheckPasswordSignInAsync(kullanici, istek.Sifre, lockoutOnFailure: true);
        if (!sonuc.Succeeded)
            return Hatali("Kullanıcı adı/e-posta veya şifre hatalı.", StatusCodes.Status401Unauthorized);

        var roller = await _userManager.GetRolesAsync(kullanici);
        var cevap = await _jwtTokenService.TokenUretAsync(kullanici, roller);

        return Basarili(cevap);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto istek)
    {
        var cevap = await _jwtTokenService.RefreshTokenIleYenileAsync(istek.AccessToken, istek.RefreshToken);
        if (cevap is null)
            return Hatali("Oturum yenilenemedi, lütfen tekrar giriş yapın.", StatusCodes.Status401Unauthorized);

        return Basarili(cevap);
    }
}
