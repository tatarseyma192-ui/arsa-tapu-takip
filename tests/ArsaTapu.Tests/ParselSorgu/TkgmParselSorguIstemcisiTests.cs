using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.Tests.TestYardimcilari;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArsaTapu.Tests.ParselSorgu;

/// <summary>
/// TkgmParselSorguIstemcisi'nin il/ilçe/mahalle çözümleme + önbellekleme mantığını, gerçek
/// HAR kaydından doğrulanan yanıt şemasını taklit eden sahte bir HTTP handler ile test eder
/// (gerçek ağ erişimi olmadan). Gerçek TKGM sunucusuna karşı davranışın BİREBİR aynı olacağının
/// garantisi değildir — yalnızca istemcinin ayrıştırma/eşleştirme/önbellekleme mantığını doğrular.
/// </summary>
public class TkgmParselSorguIstemcisiTests
{
    private static (TkgmParselSorguIstemcisi Istemci, SahteTkgmHttpHandler Handler, ITkgmIdCache IdCache) KurulumYap()
    {
        var handler = new SahteTkgmHttpHandler();
        var httpClient = new HttpClient(handler)
        {
            // Sondaki "/" KRİTİK: TkgmParselSorguIstemcisi'ndeki göreli yollar başta "/" OLMADAN
            // yazılıyor (ör. "idariYapi/ilceListe/49") — .NET'in Uri birleştirme kuralına göre bu,
            // yalnızca BaseAddress "/" ile bitiyorsa doğru şekilde EKLENİR, aksi halde BaseAddress'in
            // tüm path'i silinip yerine geçer (bkz. Program.cs'teki aynı gerekçe).
            BaseAddress = new Uri("https://cbsapi.tkgm.gov.tr/megsiswebapi.v3.1/api/")
        };
        var hizSinirlayici = new SahteHizSinirlayici();
        var idCache = new TkgmIdCache();

        var istemci = new TkgmParselSorguIstemcisi(
            httpClient, hizSinirlayici, idCache, NullLogger<TkgmParselSorguIstemcisi>.Instance);

        return (istemci, handler, idCache);
    }

    [Fact]
    public async Task SorgulaAsync_TumIstekUrlleriTabanYoluKorur()
    {
        var (istemci, handler, _) = KurulumYap();

        await istemci.SorgulaAsync("Gaziantep", "Şehitkamil", "Göksüncük", 160, 9);

        // KRİTİK REGRESYON TESTİ: göreli yollar başta "/" olmadan yazıldığı için, BaseAddress
        // sonunda "/" yoksa .NET'in Uri birleştirme kuralı taban path'i ("megsiswebapi.v3.1/api")
        // SESSİZCE düşürür. Bu test, o hatanın geri gelmesini engeller.
        Assert.NotEmpty(handler.IstenenUrller);
        Assert.All(handler.IstenenUrller, uri =>
            Assert.Contains("/megsiswebapi.v3.1/api/", uri.ToString()));
    }

    [Fact]
    public async Task SorgulaAsync_GecerliIlIlceMahalleIcinBasariliSonucDoner()
    {
        var (istemci, handler, _) = KurulumYap();

        var sonuc = await istemci.SorgulaAsync("Gaziantep", "Şehitkamil", "Göksüncük", 160, 9);

        Assert.True(sonuc.Basarili);
        Assert.NotNull(sonuc.KmlBaytlari);
        Assert.Equal(1, handler.IlceListeCagriSayisi);
        Assert.Equal(1, handler.MahalleListeCagriSayisi);
        Assert.Equal(1, handler.ParselCagriSayisi);
        Assert.Equal(1, handler.KmlIndirCagriSayisi);
    }

    [Fact]
    public async Task SorgulaAsync_BilinmeyenIlIcinHicHttpIstegiYapmadanBasarisizDoner()
    {
        var (istemci, handler, _) = KurulumYap();

        var sonuc = await istemci.SorgulaAsync("BöyleBirİlYok", "İlçe", "Mahalle", 1, 1);

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.HataMesaji);
        // İl hardcoded tablodan bulunamadığı için TKGM'e hiç istek atılmamalı.
        Assert.Equal(0, handler.IlceListeCagriSayisi);
    }

    [Fact]
    public async Task SorgulaAsync_BilinmeyenIlceIcinBasarisizDoner()
    {
        var (istemci, _, _) = KurulumYap();

        var sonuc = await istemci.SorgulaAsync("Gaziantep", "OlmayanIlce", "Göksüncük", 160, 9);

        Assert.False(sonuc.Basarili);
        Assert.Contains("OlmayanIlce", sonuc.HataMesaji);
    }

    [Fact]
    public async Task SorgulaAsync_AyniIlceIcinTekrarSorgu_IlceListesiTekrarCekilmezOnbellektenGelir()
    {
        var (istemci, handler, _) = KurulumYap();

        await istemci.SorgulaAsync("Gaziantep", "Şehitkamil", "Göksüncük", 160, 9);
        await istemci.SorgulaAsync("Gaziantep", "Şehitkamil", "Binevler", 160, 10);

        // Aynı il (Gaziantep) için ilçe listesi YALNIZCA BİR KEZ çekilmeli (önbellek).
        Assert.Equal(1, handler.IlceListeCagriSayisi);
        // Aynı ilçe (Şehitkamil) için mahalle listesi de YALNIZCA BİR KEZ çekilmeli.
        Assert.Equal(1, handler.MahalleListeCagriSayisi);
        // Ama her parsel için ayrı parsel/kml çağrısı yapılmalı.
        Assert.Equal(2, handler.ParselCagriSayisi);
        Assert.Equal(2, handler.KmlIndirCagriSayisi);
    }

    [Fact]
    public async Task SorgulaAsync_TurkceKarakterFarkliligindaDaEslesir()
    {
        var (istemci, _, _) = KurulumYap();

        // Sahte handler "Şahinbey"/"Şehitkamil" döndürüyor; girdi büyük/küçük harf ve
        // TKGM'nin kendi verisindeki tutarsızlıklara (ör. noktasız 'i') toleranslı olmalı.
        var sonuc = await istemci.SorgulaAsync("GAZIANTEP", "şehitkamil", "göksüncük", 160, 9);

        Assert.True(sonuc.Basarili);
    }
}
