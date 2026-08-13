using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Business.TasinmazYukleme.Pdf;
using Xunit;

namespace ArsaTapu.Tests.TasinmazYukleme;

/// <summary>
/// PdfPig'e bağımlı OLMAYAN saf algoritma testleri. Senaryo, Python ile önceden
/// doğrulanan simülasyonla BİREBİR AYNIDIR (satır kümeleme, sütun sınırı tespiti,
/// boş Bağımsız Bölüm No'nun sütun kaymasına yol açmadığının doğrulanması).
/// </summary>
public class TabloSatirOlusturucuTests
{
    private static List<KonumluKelime> SatirUret(double ustY, double altY, (string Metin, double SolX, double SagX)[] kelimeler) =>
        kelimeler.Select(k => new KonumluKelime(k.Metin, k.SolX, k.SagX, ustY, altY)).ToList();

    private static List<KonumluKelime> OrnekBaslikSatiri() => SatirUret(700, 692, new[]
    {
        ("Taşınmaz", 10.0, 55.0), ("No", 57.0, 70.0),
        ("Nitelik", 95.0, 130.0),
        ("İl", 155.0, 165.0),
        ("İlçe", 190.0, 205.0),
        ("Mahalle", 230.0, 270.0),
        ("Yüzölçüm", 300.0, 340.0),
        ("Ada", 365.0, 380.0),
        ("Parsel", 405.0, 430.0),
        ("Bağımsız", 460.0, 500.0), ("Bölüm", 502.0, 525.0), ("No", 527.0, 538.0),
        ("Zemin", 565.0, 590.0), ("Hisse", 592.0, 615.0), ("ID", 617.0, 628.0)
    });

    [Fact]
    public void SatirlaraGrupla_UcSatiriDogruAyirir()
    {
        var baslik = OrnekBaslikSatiri();
        var veri1 = SatirUret(685, 677, new[]
        {
            ("13425953", 10.0, 65.0), ("Arsa", 95.0, 120.0), ("Gaziantep", 150.0, 200.0),
            ("Şahinbey", 190.0, 230.0), ("Binevler", 230.0, 270.0), ("318,50", 305.0, 340.0),
            ("171", 368.0, 382.0), ("190", 405.0, 420.0), ("4", 495.0, 500.0), ("Z-01", 580.0, 610.0)
        });

        var tumKelimeler = baslik.Concat(veri1).ToList();
        var satirlar = TabloSatirOlusturucu.SatirlaraGrupla(tumKelimeler);

        Assert.Equal(2, satirlar.Count);
        Assert.Equal(baslik.Count, satirlar[0].Count);
        Assert.Equal(veri1.Count, satirlar[1].Count);
    }

    [Fact]
    public void SutunSinirlariniHesapla_OnSutunUretir()
    {
        var baslik = OrnekBaslikSatiri();
        var sinirlar = TabloSatirOlusturucu.SutunSinirlariniHesapla(baslik, 10);

        Assert.Equal(10, sinirlar.Count);
    }

    [Fact]
    public void SatiriSutunlaraAyir_BosBagimsizBolumNoSutunKaymasinaYolAcmaz()
    {
        var baslik = OrnekBaslikSatiri();
        var sinirlar = TabloSatirOlusturucu.SutunSinirlariniHesapla(baslik, 10);

        // Kişi B'nin kaydı: Bağımsız Bölüm No BOŞ (Requirements madde 2 — "boş olabilir").
        var veri2 = SatirUret(670, 662, new[]
        {
            ("22110045", 10.0, 65.0), ("Arsa", 95.0, 120.0), ("Gaziantep", 150.0, 200.0),
            ("Şahinbey", 190.0, 230.0), ("Binevler", 230.0, 270.0), ("450,00", 305.0, 340.0),
            ("171", 368.0, 382.0), ("190", 405.0, 420.0),
            // Bağımsız Bölüm No sütununda hiç kelime yok.
            ("Z-01", 580.0, 610.0)
        });

        var sutunlar = TabloSatirOlusturucu.SatiriSutunlaraAyir(veri2, sinirlar);

        Assert.Equal("22110045", sutunlar[0]);
        Assert.Equal("Arsa", sutunlar[1]);
        Assert.Equal("171", sutunlar[6]);
        Assert.Equal("190", sutunlar[7]);
        Assert.Equal("", sutunlar[8]); // Bağımsız Bölüm No boş kalmalı
        Assert.Equal("Z-01", sutunlar[9]); // kaymadan doğru (son) sütuna düşmeli
    }

    [Fact]
    public void BaslikSatiriniBul_BaslikSatiriniDogruTespitEder()
    {
        var baslik = OrnekBaslikSatiri();
        var veri1 = SatirUret(685, 677, new[] { ("13425953", 10.0, 65.0), ("Arsa", 95.0, 120.0) });

        var satirlar = new List<List<KonumluKelime>> { veri1, baslik }; // kasıtlı ters sıra

        var index = TabloSatirOlusturucu.BaslikSatiriniBul(satirlar);

        Assert.Equal(1, index);
    }

    // === Aşağıdaki testler, 2026-08-04'te sağlanan GERÇEK bir WebTapu PDF'inde bulunan iki
    // gerçek soruna dayanır: (1) büyük/döndürülmüş bir filigranın gerçek metinle karışması,
    // (2) uzun Nitelik/İlçe değerlerinin birden fazla satıra yayılması. Sayısal değerler
    // (Yukseklik ~100+ vs ~12, boşluk oranları) gerçek PDF'ten ölçülen değerlerle uyumludur. ===

    [Fact]
    public void HarflerdenKelimeOlustur_BuyukFiligranKarakteriniAyiklar()
    {
        // Gerçek örnekte oldugu gibi: normal 12pt metnin ORTASINA denk gelen dev bir filigran
        // harfi ("Ç", 130pt) iki gerçek değeri ("241.51" ve "1093") birbirine kaynaştırmaya
        // çalışıyor — filigran karakter düzeyinde ayıklanınca ikisi doğru ayrı kelimeler olarak kalmalı.
        var harfler = new List<KonumluHarf>();
        void EkleKelime(string kelime, double solX, double yukseklik = 12.0, double ustY = 163.3)
        {
            var x = solX;
            foreach (var c in kelime)
            {
                harfler.Add(new KonumluHarf(c.ToString(), x, x + 6.0, ustY, ustY - yukseklik));
                x += 6.0;
            }
        }

        EkleKelime("241.51", 440.5);
        // Filigran: aynı civarda ama ÇOK daha büyük yükseklikte tek bir "Ç" harfi.
        harfler.Add(new KonumluHarf("Ç", 474.0, 594.7, 293.8, 164.0));
        EkleKelime("1093", 604.4);

        var kelimeler = TabloSatirOlusturucu.HarflerdenKelimeOlustur(harfler);

        Assert.DoesNotContain(kelimeler, k => k.Metin.Contains('Ç'));
        Assert.Contains(kelimeler, k => k.Metin == "241.51");
        Assert.Contains(kelimeler, k => k.Metin == "1093");
    }

    [Fact]
    public void HarflerdenKelimeOlustur_GercekKelimeArasiBosluguDogruAyirir()
    {
        // Gerçek örnekte "1" ve "KATLI" arasındaki boşluk (~3pt, 12pt yazıda) yanlışlıkla TEK
        // kelime olarak birleşmişti — eşik karakter yüksekliğiyle orantılı olmalı.
        var harfler = new List<KonumluHarf>
        {
            new("1", 134.0, 140.8, 439.3, 427.3),
            new("K", 143.7, 151.2, 439.3, 427.3),
            new("A", 151.2, 159.0, 439.3, 427.3),
            new("T", 159.0, 165.5, 439.3, 427.3),
            new("L", 165.5, 172.0, 439.3, 427.3),
            new("I", 172.0, 175.2, 439.3, 427.3)
        };

        var kelimeler = TabloSatirOlusturucu.HarflerdenKelimeOlustur(harfler);

        Assert.Equal(2, kelimeler.Count);
        Assert.Equal("1", kelimeler[0].Metin);
        Assert.Equal("KATLI", kelimeler[1].Metin);
    }

    [Fact]
    public void SatirParcalariniBirlestir_UzunNitelikDegeriniOncekiSatiraBirlestirir()
    {
        // Sütun sırası: TasinmazNo=0, Nitelik=1, Il=2, Ilce=3, Mahalle=4, Yuzolcum=5, Ada=6,
        // Parsel=7, BagimsizBolumNo=8, ZeminHisseId=9 (KanonikSutunlar.Tumu ile aynı sıra).
        var satirlar = new List<(double OrtalamaY, string[] Sutunlar)>
        {
            (439.3, new[] { "13185611", "1 KATLI", "GAZİANTEP", "ŞAHİNBEY", "BİNEVLER", "784", "169", "182", "", "122858455" }),
            (453.3, new[] { "", "OTO GALERİ", "", "", "", "", "", "", "", "" }),   // devam parçası (~14pt boşluk)
            (467.4, new[] { "", "2 MESKENLİ", "", "", "", "", "", "", "", "" }),  // devam parçası
            (481.4, new[] { "", "KARGİR", "", "", "", "", "", "", "", "" }),      // devam parçası
            (495.5, new[] { "", "BİNA", "", "", "", "", "", "", "", "" }),        // devam parçası
            (514.6, new[] { "13185612", "BİR", "GAZİANTEP", "ŞAHİNBEY", "BİNEVLER", "711", "169", "184", "", "122858457" }),
            (557.3, new[] { "", "", "", "", "", "", "", "", "", "1/5" })           // sayfa altbilgisi (~29pt buyuk bosluk) — BİRLEŞTİRİLMEMELİ
        };

        var anahtarIndeksler = new[] { 0, 6, 7, 9 }; // TasinmazNo, Ada, Parsel, ZeminHisseId
        var esik = 12.0 * 2.0; // gerçek örnekte ortalama kelime yüksekliği ~12, eşik ~24

        var sonuc = TabloSatirOlusturucu.SatirParcalariniBirlestir(satirlar, anahtarIndeksler, esik);

        Assert.Equal(2, sonuc.Count); // yalnızca 2 GERÇEK satır kalmalı, devam parçaları ayrı satır değil
        Assert.Equal("1 KATLI OTO GALERİ 2 MESKENLİ KARGİR BİNA", sonuc[0][1]);
        Assert.Equal("BİR", sonuc[1][1]);

        // Sayfa altbilgisi ("1/5") hiçbir satıra karışmamalı (ZeminHisseId hâlâ orijinal).
        Assert.Equal("122858457", sonuc[1][9]);
    }

    [Fact]
    public void SatirParcalariniBirlestir_IlkSatirdanOnceGelenGurultuyuAtar()
    {
        // Gerçek örnekte sayfa başlığı/açıklaması gibi hiçbir gerçek satırdan ÖNCE gelen
        // anahtarsız satırlar (henüz birleştirilecek bir "önceki gerçek satır" yokken) sessizce atlanmalı.
        var satirlar = new List<(double OrtalamaY, string[] Sutunlar)>
        {
            (26.6, new[] { "", "", "BAŞLIK METNİ", "", "", "", "", "", "", "" }),
            (144.2, new[] { "8027710", "Mesken", "MERSİN", "ERDEMLİ", "ÇEŞMELİ", "31266", "121", "1", "18", "17827152" })
        };

        var sonuc = TabloSatirOlusturucu.SatirParcalariniBirlestir(satirlar, new[] { 0, 6, 7, 9 }, 24.0);

        Assert.Single(sonuc);
        Assert.Equal("8027710", sonuc[0][0]);
    }
}
