using ArsaTapu.Business.TasinmazYukleme;
using Xunit;

namespace ArsaTapu.Tests.TasinmazYukleme;

public class SatirDonusturucuTests
{
    private static Dictionary<string, string?> GecerliHamSatir(string? bagimsizBolumNo = "4", string yuzolcum = "318,50")
    {
        return new Dictionary<string, string?>
        {
            [KanonikSutunlar.TasinmazNo] = "13425953",
            [KanonikSutunlar.Nitelik] = "Arsa",
            [KanonikSutunlar.Il] = "Gaziantep",
            [KanonikSutunlar.Ilce] = "Şahinbey",
            [KanonikSutunlar.Mahalle] = "Binevler",
            [KanonikSutunlar.Yuzolcum] = yuzolcum,
            [KanonikSutunlar.Ada] = "171",
            [KanonikSutunlar.Parsel] = "190",
            [KanonikSutunlar.BagimsizBolumNo] = bagimsizBolumNo,
            [KanonikSutunlar.ZeminHisseId] = "Z-01"
        };
    }

    [Fact]
    public void Donustur_GecerliSatiriDogruCevirir()
    {
        var (sonuc, hata) = SatirDonusturucu.Donustur(GecerliHamSatir(), 1);

        Assert.Null(hata);
        Assert.NotNull(sonuc);
        Assert.Equal("13425953", sonuc!.TasinmazNo);
        Assert.Equal(4, sonuc.BagimsizBolumNo);
        Assert.Equal(318.50m, sonuc.Yuzolcum);
        Assert.Equal(171, sonuc.Ada);
        Assert.Equal(190, sonuc.Parsel);
    }

    [Fact]
    public void Donustur_BosBagimsizBolumNoNullOlarakAtanir()
    {
        var (sonuc, hata) = SatirDonusturucu.Donustur(GecerliHamSatir(bagimsizBolumNo: ""), 2);

        Assert.Null(hata);
        Assert.Null(sonuc!.BagimsizBolumNo);
    }

    [Fact]
    public void Donustur_TurkceOndalikBicimiDogruParseEder()
    {
        var (sonuc, _) = SatirDonusturucu.Donustur(GecerliHamSatir(yuzolcum: "1.234,56"), 3);
        Assert.Equal(1234.56m, sonuc!.Yuzolcum);
    }

    [Fact]
    public void Donustur_IngilizceOndalikBicimiDogruParseEder()
    {
        var (sonuc, _) = SatirDonusturucu.Donustur(GecerliHamSatir(yuzolcum: "1,234.56"), 4);
        Assert.Equal(1234.56m, sonuc!.Yuzolcum);
    }

    [Fact]
    public void Donustur_EksikTasinmazNoSatiriKabulEderNullOlarakAtar()
    {
        // 2026-08-04'te doğrulandı: gerçek bir Excel örneğinde bu sütun hiç yoktu. TasinmazNo
        // mülkiyet tekilleştirme anahtarının parçası DEĞİLDİR — eksikse satır REDDEDİLMEMELİ.
        var ham = GecerliHamSatir();
        ham[KanonikSutunlar.TasinmazNo] = "";

        var (sonuc, hata) = SatirDonusturucu.Donustur(ham, 5);

        Assert.Null(hata);
        Assert.NotNull(sonuc);
        Assert.Null(sonuc!.TasinmazNo);
    }

    [Fact]
    public void Donustur_TasinmazNoSutunuHicYoksaSessizceNullOlarakGecer()
    {
        // Gerçek Excel örneğinde (2026-08-04) bu sütun HİÇ YOKTU — sözlükte anahtar bile
        // bulunmuyor (boş değer değil, TAMAMEN YOK). Bu, ExcelSatirCikarici'nin ürettiği
        // gerçek durumu birebir taklit eder.
        var ham = GecerliHamSatir();
        ham.Remove(KanonikSutunlar.TasinmazNo);

        var (sonuc, hata) = SatirDonusturucu.Donustur(ham, 9);

        Assert.Null(hata);
        Assert.NotNull(sonuc);
        Assert.Null(sonuc!.TasinmazNo);
    }

    [Fact]
    public void Donustur_SayisalOlmayanAdaSatiriReddeder()
    {
        var ham = GecerliHamSatir();
        ham[KanonikSutunlar.Ada] = "Ada"; // tekrarlanan başlık satırı simülasyonu

        var (sonuc, hata) = SatirDonusturucu.Donustur(ham, 6);

        Assert.Null(sonuc);
        Assert.NotNull(hata);
        Assert.Contains("Ada", hata);
    }

    // === Aşağıdaki iki test, 2026-08-04'te sağlanan GERÇEK bir Excel örneğinde bulunan iki
    // gerçek hataya dayanır: (1) boş Bağımsız Bölüm No, boş hücre yerine literal "-" ile
    // işaretleniyordu; (2) Excel'den gelen native sayı hücreleri ".0" son ekiyle gelebiliyordu,
    // eski (yalnızca rakamları koruyan) ayrıştırma bunu SESSİZCE yanlış bir sayıya çeviriyordu
    // (ör. "182.0" -> 1820). ===

    [Fact]
    public void Donustur_TireIsaretiBosBagimsizBolumNoOlarakKabulEdilir()
    {
        var (sonuc, hata) = SatirDonusturucu.Donustur(GecerliHamSatir(bagimsizBolumNo: "-"), 7);

        Assert.Null(hata);
        Assert.Null(sonuc!.BagimsizBolumNo);
    }

    [Fact]
    public void Donustur_OndalikSonEkliTamSayiDogruAyristirilir()
    {
        var ham = GecerliHamSatir(bagimsizBolumNo: "-");
        ham[KanonikSutunlar.Ada] = "182.0"; // Excel native sayı hücresinden gelebilecek biçim

        var (sonuc, hata) = SatirDonusturucu.Donustur(ham, 8);

        Assert.Null(hata);
        Assert.Equal(182, sonuc!.Ada); // ESKİ hatalı davranış: 1820 üretirdi
    }
}
