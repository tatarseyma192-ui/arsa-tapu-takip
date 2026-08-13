using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArsaTapu.DataAccess.Context;
using ArsaTapu.DataAccess.Entities;
using ArsaTapu.DataAccess.Identity;
using ArsaTapu.Dto.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ArsaTapu.Api.Services;

/// <summary>
/// Technical Defaults madde 5: JWT Bearer (Access Token + Refresh Token).
/// Access Token kısa ömürlü; Refresh Token ile yenilenir, tek kullanımlıktır (kullanılınca iptal edilir).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ArsaTapuDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IConfiguration configuration, ArsaTapuDbContext context, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _context = context;
        _userManager = userManager;
    }

    public async Task<LoginResponseDto> TokenUretAsync(ApplicationUser kullanici, IEnumerable<string> roller)
    {
        var rolListesi = roller.ToList();
        var (accessToken, sonGecerlilik) = AccessTokenUret(kullanici, rolListesi);
        var refreshToken = await RefreshTokenUretVeKaydetAsync(kullanici.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            AccessTokenSonGecerlilik = sonGecerlilik,
            RefreshToken = refreshToken,
            AdSoyad = kullanici.AdSoyad ?? kullanici.UserName ?? string.Empty,
            Roller = rolListesi
        };
    }

    public async Task<LoginResponseDto?> RefreshTokenIleYenileAsync(string accessToken, string refreshToken)
    {
        var principal = SuresiDolmusTokenPrincipalGetir(accessToken);
        if (principal is null) return null;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;

        var kayitliToken = await _context.RefreshTokenlar
            .FirstOrDefaultAsync(x => x.Token == refreshToken && x.UserId == userId && !x.Iptal);

        if (kayitliToken is null || kayitliToken.SonGecerlilikTarihi < DateTime.UtcNow)
            return null;

        var kullanici = await _userManager.FindByIdAsync(userId);
        if (kullanici is null) return null;

        var roller = await _userManager.GetRolesAsync(kullanici);

        kayitliToken.Iptal = true; // eski refresh token tek kullanımlık
        await _context.SaveChangesAsync();

        return await TokenUretAsync(kullanici, roller);
    }

    private (string Token, DateTime SonGecerlilik) AccessTokenUret(ApplicationUser kullanici, IReadOnlyList<string> roller)
    {
        var anahtarBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var dakika = int.Parse(_configuration["Jwt:AccessTokenDakika"] ?? "30");
        var sonGecerlilik = DateTime.UtcNow.AddMinutes(dakika);

        var claimListesi = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id),
            new(JwtRegisteredClaimNames.Sub, kullanici.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(kullanici.UserName))
            claimListesi.Add(new Claim(ClaimTypes.Name, kullanici.UserName));

        claimListesi.AddRange(roller.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claimListesi,
            expires: sonGecerlilik,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(anahtarBytes), SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), sonGecerlilik);
    }

    private async Task<string> RefreshTokenUretVeKaydetAsync(string userId)
    {
        var gunSayisi = int.Parse(_configuration["Jwt:RefreshTokenGun"] ?? "7");
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        _context.RefreshTokenlar.Add(new RefreshToken
        {
            UserId = userId,
            Token = token,
            OlusturmaTarihi = DateTime.UtcNow,
            SonGecerlilikTarihi = DateTime.UtcNow.AddDays(gunSayisi),
            Iptal = false
        });
        await _context.SaveChangesAsync();

        return token;
    }

    private ClaimsPrincipal? SuresiDolmusTokenPrincipalGetir(string token)
    {
        var anahtarBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var parametreler = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(anahtarBytes),
            ValidateLifetime = false // süresi dolmuş access token'ın claim'lerini okuyabilmek için
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parametreler, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
