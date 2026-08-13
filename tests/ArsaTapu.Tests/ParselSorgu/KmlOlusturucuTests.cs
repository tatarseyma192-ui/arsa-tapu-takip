using System.Text;
using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.Dto.ParselKml;
using ArsaTapu.Tests.TestYardimcilari;
using Xunit;

namespace ArsaTapu.Tests.ParselSorgu;

public class KmlOlusturucuTests
{
    private static readonly byte[] OrnekKmlBaytlari = Encoding.UTF8.GetBytes(SahteParselSorguIstemcisi.OrnekKml);

    [Fact]
    public void AciklamaEnjekteEt_TasinmazNoVeBagimsizBolumleriDescriptionaYazar()
    {
        var referanslar = new List<TasinmazReferansDto>
        {
            new() { TasinmazNo = "13425953", BagimsizBolumNo = 2 },
            new() { TasinmazNo = "13423289", BagimsizBolumNo = 4 }
        };

        var sonucBaytlari = KmlOlusturucu.AciklamaEnjekteEt(OrnekKmlBaytlari, referanslar);
        var metin = Encoding.UTF8.GetString(sonucBaytlari);

        Assert.Contains("Bağımsız Bölümler: 2, 4", metin);
        Assert.Contains("Taşınmaz No: 13425953, 13423289", metin);
        // Orijinal geometri korunmalı — enjeksiyon yalnızca description ekler, coordinates'i bozmaz.
        Assert.Contains("<coordinates>", metin);
        Assert.Contains("37.0,37.0,0", metin);
    }

    [Fact]
    public void AciklamaEnjekteEt_GecerliXmlUretmeyeDevamEder()
    {
        var referanslar = new List<TasinmazReferansDto> { new() { TasinmazNo = "T-1", BagimsizBolumNo = null } };

        var sonucBaytlari = KmlOlusturucu.AciklamaEnjekteEt(OrnekKmlBaytlari, referanslar);

        using var akis = new MemoryStream(sonucBaytlari);
        var belge = System.Xml.Linq.XDocument.Load(akis); // parse edilemezse istisna fırlatır
        Assert.NotNull(belge.Root);
    }

    [Fact]
    public void AciklamaEnjekteEt_BozukKmlIcinOrijinaliDegistirmedenDoner()
    {
        var bozukBaytlar = Encoding.UTF8.GetBytes("bu gecerli bir XML degil <<<");
        var referanslar = new List<TasinmazReferansDto> { new() { TasinmazNo = "T-1" } };

        var sonuc = KmlOlusturucu.AciklamaEnjekteEt(bozukBaytlar, referanslar);

        // Ayrıştırılamadığında orijinal bayt dizisi AYNEN korunmalı (asıl dosya asla bozulmamalı).
        Assert.Equal(bozukBaytlar, sonuc);
    }

    [Fact]
    public void AciklamaEnjekteEt_BosReferansListesindeOrijinaliDegistirmedenDoner()
    {
        var sonuc = KmlOlusturucu.AciklamaEnjekteEt(OrnekKmlBaytlari, new List<TasinmazReferansDto>());
        Assert.Equal(OrnekKmlBaytlari, sonuc);
    }
}
