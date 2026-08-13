using ArsaTapu.Business.Validators;
using ArsaTapu.Dto.Auth;
using Xunit;

namespace ArsaTapu.Tests;

/// <summary>
/// Kullanıcı isteği üzerine eklenen kullanıcı oluşturma özelliğinin doğrulama kuralı:
/// Rol="Patron" iken KisiId zorunlu (hesabı hangi kişiye bağlayacağımızı belirtir),
/// diğer rollerde (Admin/Personel) zorunlu değil.
/// </summary>
public class KullaniciOlusturDtoValidatorTests
{
    private readonly KullaniciOlusturDtoValidator _validator = new();

    private static KullaniciOlusturDto GecerliIstek(string rol, int? kisiId = null) => new()
    {
        Eposta = "test@ornek.com",
        Sifre = "GucluSifre123!",
        AdSoyad = "Test Kullanıcı",
        Rol = rol,
        KisiId = kisiId
    };

    [Fact]
    public void PatronRolundeKisiIdEksikseGecersiz()
    {
        var sonuc = _validator.Validate(GecerliIstek("Patron", kisiId: null));

        Assert.False(sonuc.IsValid);
        Assert.Contains(sonuc.Errors, e => e.PropertyName == nameof(KullaniciOlusturDto.KisiId));
    }

    [Fact]
    public void PatronRolundeKisiIdVarsaGecerli()
    {
        var sonuc = _validator.Validate(GecerliIstek("Patron", kisiId: 5));
        Assert.True(sonuc.IsValid);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Personel")]
    public void AdminVeyaPersonelRolundeKisiIdOlmadanDaGecerli(string rol)
    {
        var sonuc = _validator.Validate(GecerliIstek(rol, kisiId: null));
        Assert.True(sonuc.IsValid);
    }

    [Fact]
    public void GecersizRolAdiReddedilir()
    {
        var sonuc = _validator.Validate(GecerliIstek("BoyleBirRolYok"));
        Assert.False(sonuc.IsValid);
    }

    [Fact]
    public void GecersizEpostaReddedilir()
    {
        var istek = GecerliIstek("Admin");
        istek.Eposta = "gecerli-olmayan-eposta";

        var sonuc = _validator.Validate(istek);
        Assert.False(sonuc.IsValid);
    }

    [Fact]
    public void KisaSifreReddedilir()
    {
        var istek = GecerliIstek("Admin");
        istek.Sifre = "1234";

        var sonuc = _validator.Validate(istek);
        Assert.False(sonuc.IsValid);
    }
}
