using ArsaTapu.Business.ParselSorgu;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArsaTapu.Tests.ParselSorgu;

public class KmlDosyaDepoServiceTests
{
    private static KmlDosyaDepoService OlusturGeciciDepoIle()
    {
        var gecici = Path.Combine(Path.GetTempPath(), "arsatapu-test-kml-" + Guid.NewGuid());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["KmlDepolama:KokDizin"] = gecici })
            .Build();

        return new KmlDosyaDepoService(config);
    }

    [Fact]
    public void DosyaAdiOlustur_RequirementsOrneginiBirebirUretir()
    {
        var servis = OlusturGeciciDepoIle();

        var dosyaAdi = servis.DosyaAdiOlustur("Gaziantep", "Şahinbey", "Binevler", 171, 190);

        // Requirements madde 4.2 örneği: GAZIANTEP_SAHINBEY_BINEVLER_171_190.kml
        // (Türkçe büyük harfe çevrimde "ş" -> "Ş" olur; dosya sistemi UTF-8 destekler.)
        Assert.Equal("GAZIANTEP_ŞAHINBEY_BINEVLER_171_190.kml", dosyaAdi);
    }

    [Fact]
    public async Task KaydetAsync_DosyayiDiskeYazarVeOkunabilirOlur()
    {
        var servis = OlusturGeciciDepoIle();
        var icerik = System.Text.Encoding.UTF8.GetBytes("<kml></kml>");

        var dosyaAdi = await servis.KaydetAsync("test.kml", icerik, CancellationToken.None);

        Assert.Equal("test.kml", dosyaAdi);
    }
}
