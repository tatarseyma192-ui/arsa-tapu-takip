using ArsaTapu.Api.Authorization;
using ArsaTapu.DataAccess.Identity;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Dto.Auth;
using ArsaTapu.Dto.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.Api.Controllers;

/// <summary>
/// Kullanıcı hesabı oluşturma/listeleme — Requirements madde 1: sisteme yeni Admin/Personel/Patron
/// hesabı yalnızca BURADAN, yalnızca Admin tarafından eklenir. AuthController yalnızca giriş/token
/// yenileme ile ilgilenir ([AllowAnonymous]), bu yüzden ayrı bir controller'da tutulur.
///
/// İlk kurulumda: appsettings'teki InitialAdmin ile oluşan tek Admin hesabıyla giriş yapılır,
/// SONRA buradaki uçlar üzerinden diğer Personel/Patron hesapları eklenir (Requirements madde 1
/// rol tanımları: Admin her şeyi yönetir, Personel yükleme yapar, Patron yalnızca kendi profilini
/// salt-okunur görür — Patron hesabı oluşturulurken KisiId ile hangi Kişi'ye bağlı olduğu belirtilir).
/// </summary>
[Route("api/kullanici")]
[Authorize(Policy = PolicyIsimleri.SadeceYonetim)]
public class KullaniciController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public KullaniciController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<IActionResult> Olustur([FromBody] KullaniciOlusturDto istek)
    {
        Kisi? kisi = null;

        if (istek.Rol == Roller.Patron)
        {
            kisi = await _unitOfWork.Kisiler.GetirAsync(istek.KisiId!.Value);
            if (kisi is null)
                return Hatali($"'{istek.KisiId}' numaralı kişi bulunamadı.", StatusCodes.Status404NotFound);
            if (!string.IsNullOrWhiteSpace(kisi.KullaniciId))
                return Hatali("Bu kişi zaten başka bir kullanıcı hesabına bağlı.");
        }

        var mevcutEposta = await _userManager.FindByEmailAsync(istek.Eposta);
        if (mevcutEposta is not null)
            return Hatali("Bu e-posta adresiyle zaten bir kullanıcı var.");

        var kullanici = new ApplicationUser
        {
            UserName = istek.Eposta,
            Email = istek.Eposta,
            AdSoyad = istek.AdSoyad,
            EmailConfirmed = true // Admin elle oluşturduğu için ayrıca e-posta doğrulaması istenmez
        };

        var olusturmaSonucu = await _userManager.CreateAsync(kullanici, istek.Sifre);
        if (!olusturmaSonucu.Succeeded)
        {
            var hatalar = olusturmaSonucu.Errors
                .Select(e => new FieldError { Field = "Sifre", Message = e.Description })
                .ToList();
            return Hatali("Kullanıcı oluşturulamadı.", StatusCodes.Status400BadRequest, hatalar);
        }

        await _userManager.AddToRoleAsync(kullanici, istek.Rol);

        if (kisi is not null)
        {
            kisi.KullaniciId = kullanici.Id;
            _unitOfWork.Kisiler.Guncelle(kisi);
            await _unitOfWork.KaydetAsync();
        }

        return Olusturuldu(nameof(Getir), new { id = kullanici.Id }, await DtoyaCevirAsync(kullanici));
    }

    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        // ASP.NET Identity'nin UserManager.Users'ı IQueryable döner; kullanıcı sayısı düşük
        // olduğu (idari bir ekran) için basit bir listeleme yeterli, sayfalama gerekmiyor.
        var kullanicilar = _userManager.Users.ToList();

        // Kisi eşleşmelerini TEK sorguda çekip bellekte eşleştiriyoruz (N+1 sorgudan kaçınmak için).
        var kisiEslesmeleri = await _unitOfWork.Kisiler.Sorgu(takipEtme: false)
            .Where(k => k.KullaniciId != null)
            .Select(k => new { k.Id, k.KullaniciId, k.AdSoyad })
            .ToListAsync();

        var sonuc = new List<KullaniciDto>();
        foreach (var kullanici in kullanicilar)
        {
            var roller = await _userManager.GetRolesAsync(kullanici);
            var kisi = kisiEslesmeleri.FirstOrDefault(k => k.KullaniciId == kullanici.Id);

            sonuc.Add(new KullaniciDto
            {
                Id = kullanici.Id,
                Eposta = kullanici.Email ?? kullanici.UserName ?? "",
                AdSoyad = kullanici.AdSoyad,
                Roller = roller.ToList(),
                KisiId = kisi?.Id,
                KisiAdSoyad = kisi?.AdSoyad
            });
        }

        return Basarili(sonuc);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Getir(string id)
    {
        var kullanici = await _userManager.FindByIdAsync(id);
        if (kullanici is null)
            return Hatali("Kullanıcı bulunamadı.", StatusCodes.Status404NotFound);

        return Basarili(await DtoyaCevirAsync(kullanici));
    }

    private async Task<KullaniciDto> DtoyaCevirAsync(ApplicationUser kullanici)
    {
        var roller = await _userManager.GetRolesAsync(kullanici);
        var kisi = await _unitOfWork.Kisiler.Sorgu(takipEtme: false)
            .FirstOrDefaultAsync(k => k.KullaniciId == kullanici.Id);

        return new KullaniciDto
        {
            Id = kullanici.Id,
            Eposta = kullanici.Email ?? kullanici.UserName ?? "",
            AdSoyad = kullanici.AdSoyad,
            Roller = roller.ToList(),
            KisiId = kisi?.Id,
            KisiAdSoyad = kisi?.AdSoyad
        };
    }
}
