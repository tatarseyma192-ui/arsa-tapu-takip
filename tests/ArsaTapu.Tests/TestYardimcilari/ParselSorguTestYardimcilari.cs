using ArsaTapu.Business.ParselSorgu;

namespace ArsaTapu.Tests.TestYardimcilari;

/// <summary>Gerçek TKGM'ye bağlanmadan test etmek için sahte istemci — başarı/başarısızlık senaryosu dışarıdan verilir.</summary>
public class SahteParselSorguIstemcisi : IParselSorguIstemcisi
{
    public const string OrnekKml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<kml xmlns=\"http://www.opengis.net/kml/2.2\"><Document><Placemark>" +
        "<name>Test Parsel</name>" +
        "<Polygon><outerBoundaryIs><LinearRing>" +
        "<coordinates>37.0,37.0,0 37.0,37.1,0 37.1,37.1,0 37.0,37.0,0</coordinates>" +
        "</LinearRing></outerBoundaryIs></Polygon>" +
        "</Placemark></Document></kml>";

    private readonly Func<string, string, string, int, int, ParselSorguIstemciSonucu> _davranis;
    public int CagriSayisi { get; private set; }
    public List<(string Il, string Ilce, string Mahalle, int Ada, int Parsel)> Cagrilar { get; } = new();

    public SahteParselSorguIstemcisi(Func<string, string, string, int, int, ParselSorguIstemciSonucu>? davranis = null)
    {
        _davranis = davranis ?? ((_, _, _, _, _) =>
            ParselSorguIstemciSonucu.BasariliSonuc(System.Text.Encoding.UTF8.GetBytes(OrnekKml)));
    }

    public Task<ParselSorguIstemciSonucu> SorgulaAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default)
    {
        CagriSayisi++;
        Cagrilar.Add((il, ilce, mahalle, ada, parsel));
        return Task.FromResult(_davranis(il, ilce, mahalle, ada, parsel));
    }
}

/// <summary>Gerçek disk erişimi olmadan test etmek için bellek-içi sahte dosya deposu.</summary>
public class SahteKmlDosyaDepoService : IKmlDosyaDepoService
{
    public Dictionary<string, byte[]> Dosyalar { get; } = new();

    public string DosyaAdiOlustur(string il, string ilce, string mahalle, int ada, int parsel) =>
        $"{il.ToUpperInvariant()}_{ilce.ToUpperInvariant()}_{mahalle.ToUpperInvariant()}_{ada}_{parsel}.kml";

    public Task<string> KaydetAsync(string dosyaAdi, byte[] icerik, CancellationToken ct = default)
    {
        Dosyalar[dosyaAdi] = icerik;
        return Task.FromResult(dosyaAdi);
    }

    public Task SilAsync(string dosyaYolu, CancellationToken ct = default)
    {
        Dosyalar.Remove(dosyaYolu);
        return Task.CompletedTask;
    }
}

/// <summary>Testlerde beklemesiz, anında geçen sahte hız sınırlayıcı.</summary>
public class SahteHizSinirlayici : IParselSorguHizSinirlayici
{
    public int CagriSayisi { get; private set; }

    public Task BeklemeSuresinceBeklaAsync(CancellationToken ct = default)
    {
        CagriSayisi++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// TkgmParselSorguIstemcisi'ni gerçek ağ erişimi olmadan test etmek için sahte HTTP handler.
/// Yanıt şekilleri gerçek HAR kaydından doğrulanan şemayla BİREBİR AYNIDIR (features/properties.id/text).
/// </summary>
public class SahteTkgmHttpHandler : HttpMessageHandler
{
    public int IlceListeCagriSayisi { get; private set; }
    public int MahalleListeCagriSayisi { get; private set; }
    public int ParselCagriSayisi { get; private set; }
    public int KmlIndirCagriSayisi { get; private set; }
    public List<Uri> IstenenUrller { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        IstenenUrller.Add(request.RequestUri!);
        var yol = request.RequestUri!.AbsolutePath;

        if (yol.Contains("/idariYapi/ilceListe/"))
        {
            IlceListeCagriSayisi++;
            return Task.FromResult(JsonYaniti(
                """{"features":[{"properties":{"text":"Şahinbey","id":440}},{"properties":{"text":"Şehitkamil","id":441}}]}"""));
        }

        if (yol.Contains("/idariYapi/mahalleListe/"))
        {
            MahalleListeCagriSayisi++;
            return Task.FromResult(JsonYaniti(
                """{"features":[{"properties":{"text":"Göksüncük","id":131747}},{"properties":{"text":"Binevler","id":131748}}]}"""));
        }

        if (yol.Contains("/parsel/download/"))
        {
            KmlIndirCagriSayisi++;
            var kmlBaytlari = System.Text.Encoding.UTF8.GetBytes(SahteParselSorguIstemcisi.OrnekKml);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(kmlBaytlari)
            });
        }

        if (yol.Contains("/parsel/"))
        {
            ParselCagriSayisi++;
            return Task.FromResult(JsonYaniti(
                """{"type":"Feature","geometry":{"type":"Polygon","coordinates":[]},"properties":{}}"""));
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage JsonYaniti(string json) => new(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };
}
