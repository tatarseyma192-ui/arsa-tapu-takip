using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Dto.Tekillestirme;
using ArsaTapu.Dto.TasinmazYukleme;
using ArsaTapu.Tests.TestYardimcilari;
using Xunit;

namespace ArsaTapu.Tests.TasinmazYukleme;

/// <summary>
/// Karşılaştırma motorunun (Requirements madde 3 + 4.1) uçtan uca testi: iki ardışık
/// yükleme arasında Yeni Alım / Satıldı / Zaten Kayıtlı ayrımının doğru yapıldığını,
/// YuklemeKaydi'nin oluşturulduğunu ve KML tetikleme listesinin üretildiğini doğrular.
/// </summary>
public class TasinmazYuklemeServiceTests
{
    private static async Task<(TasinmazYuklemeService Servis, IUnitOfWork UnitOfWork, int KisiId)> KurulumYapAsync()
    {
        var context = TestDbContextFactory.Olustur();

        var kisi = new Kisi { AdSoyad = "Kişi A" };
        context.Kisiler.Add(kisi);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(
            context,
            new KisiRepository(context),
            new TasinmazRepository(context),
            new ParselKmlRepository(context),
            new YuklemeKaydiRepository(context));

        var yetkiKapsam = new SahteYetkiKapsamService(); // Admin/Personel gibi davranır (kısıtlama yok)
        var mulkiyetTekillestirme = new MulkiyetTekillestirmeService(uow);
        var kmlTekillestirme = new KmlTekillestirmeService(uow);

        var servis = new TasinmazYuklemeService(
            uow, yetkiKapsam, mulkiyetTekillestirme, kmlTekillestirme,
            new KullanilmayanPdfSatirCikarici(), new KullanilmayanExcelSatirCikarici(), new KullanilmayanExcelUreticiService());

        return (servis, uow, kisi.Id);
    }

    private static MulkiyetAdayDto Aday(string tasinmazNo, int? bbNo, string hisseId, int ada = 171, int parsel = 190) => new()
    {
        TasinmazNo = tasinmazNo,
        BagimsizBolumNo = bbNo,
        ZeminHisseId = hisseId,
        Nitelik = "Arsa",
        Il = "İl 1",
        Ilce = "İlçe 1",
        Mahalle = "Mahalle 1",
        Ada = ada,
        Parsel = parsel,
        Yuzolcum = 500
    };

    [Fact]
    public async Task IlkYukleme_TumSatirlarYeniAlimOlarakEklenirVeAktifOlur()
    {
        var (servis, uow, kisiId) = await KurulumYapAsync();

        var istek = new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "ilk_yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto> { Aday("T-1", 4, "Z-01"), Aday("T-2", 5, "Z-02") }
        };

        var sonuc = await servis.OnaylaVeIsleAsync(istek, "kullanici-1");

        Assert.Equal(2, sonuc.YeniAlimSayisi);
        Assert.Equal(0, sonuc.SatildiSayisi);
        Assert.Equal(0, sonuc.ZatenKayitliSayisi);
        Assert.True(sonuc.YuklemeKaydiId > 0);

        var tasinmazlar = uow.Tasinmazlar.Sorgu(takipEtme: false).ToList();
        Assert.Equal(2, tasinmazlar.Count);
        Assert.All(tasinmazlar, t => Assert.Equal(TasinmazDurum.Aktif, t.Durum));
    }

    [Fact]
    public async Task IkinciYukleme_EksikOlanSatildiOlur_YeniOlanEklenir_MevcutOlanAktifKalir()
    {
        var (servis, uow, kisiId) = await KurulumYapAsync();

        await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "ilk_yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto> { Aday("T-1", 4, "Z-01"), Aday("T-2", 5, "Z-02") }
        }, "kullanici-1");

        // İkinci yükleme: T-1 hâlâ var (zaten kayıtlı), T-2 YOK (satıldı), T-3 yeni.
        var ikinciSonuc = await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "ikinci_yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto> { Aday("T-1", 4, "Z-01"), Aday("T-3", 6, "Z-03") }
        }, "kullanici-1");

        Assert.Equal(1, ikinciSonuc.YeniAlimSayisi); // T-3
        Assert.Equal(1, ikinciSonuc.SatildiSayisi); // T-2
        Assert.Equal(1, ikinciSonuc.ZatenKayitliSayisi); // T-1

        var tasinmazlar = uow.Tasinmazlar.Sorgu(takipEtme: false).ToList();
        Assert.Equal(3, tasinmazlar.Count); // T-2 SİLİNMEDİ, geçmişte kaldı (Requirements madde 3)

        var t1 = tasinmazlar.Single(t => t.TasinmazNo == "T-1");
        var t2 = tasinmazlar.Single(t => t.TasinmazNo == "T-2");
        var t3 = tasinmazlar.Single(t => t.TasinmazNo == "T-3");

        Assert.Equal(TasinmazDurum.Aktif, t1.Durum);
        Assert.Equal(TasinmazDurum.Satildi, t2.Durum); // silinmedi, durumu değişti
        Assert.Equal(TasinmazDurum.Aktif, t3.Durum);

        Assert.Equal(ikinciSonuc.YuklemeKaydiId, t1.SonGorulduguYuklemeId); // hâlâ görüldü -> güncellendi
        Assert.NotEqual(ikinciSonuc.YuklemeKaydiId, t2.SonGorulduguYuklemeId); // görülmedi -> DEĞİŞMEDİ
    }

    [Fact]
    public async Task Onayla_KmlSorgulanmasiGerekenParselleriDogruUretir()
    {
        var (servis, _, kisiId) = await KurulumYapAsync();

        // Aynı ada/parsel'e bağlı 2 farklı taşınmaz -> KML anahtarı (Il+Ilce+Mahalle+Ada+Parsel)
        // yalnızca 1 KEZ sorgu listesine girmeli (Requirements madde 4.2).
        var sonuc = await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto>
            {
                Aday("T-1", 4, "Z-01", ada: 100, parsel: 5),
                Aday("T-2", 9, "Z-09", ada: 100, parsel: 5)
            }
        }, "kullanici-1");

        Assert.Single(sonuc.KmlSorgulanmasiGerekenParseller);
        Assert.Equal(100, sonuc.KmlSorgulanmasiGerekenParseller[0].Ada);
        Assert.Equal(5, sonuc.KmlSorgulanmasiGerekenParseller[0].Parsel);
    }

    [Fact]
    public async Task KismiYukleme_FarkliIlIlcedekiMevcutTasinmazSatildiIsaretlenmez()
    {
        // Kullanıcı isteği: "bazen sadece belirli şehirdeki kayıtları yüklemek gerekir, hepsi
        // yüklenmez." TamPortfoyMu VARSAYILAN false — bu yükleme yalnızca "İl 1/İlçe 1" hakkında
        // konuşuyor, "İl 2/İlçe 2"deki mevcut taşınmaza HİÇ DOKUNULMAMALI.
        var (servis, uow, kisiId) = await KurulumYapAsync();

        await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "il1_yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto>
            {
                new()
                {
                    TasinmazNo = "T-IL2", BagimsizBolumNo = 1, ZeminHisseId = "Z-IL2",
                    Nitelik = "Arsa", Il = "İl 2", Ilce = "İlçe 2", Mahalle = "Mahalle 2",
                    Ada = 200, Parsel = 10, Yuzolcum = 500
                }
            }
        }, "kullanici-1");

        // İkinci yükleme: SADECE "İl 1/İlçe 1" için (TamPortfoyMu belirtilmedi -> false/varsayılan).
        // "İl 2/İlçe 2"deki taşınmazdan bu yüklemede hiç bahsedilmiyor.
        var ikinciSonuc = await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "il1_sadece.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto> { Aday("T-1", 4, "Z-01") } // Il="İl 1"/Ilce="İlçe 1"
        }, "kullanici-1");

        Assert.False(ikinciSonuc.TamPortfoyMu);
        Assert.Equal(0, ikinciSonuc.SatildiSayisi); // İl 2'deki taşınmaz kapsam dışı, dokunulmadı

        var il2Tasinmaz = uow.Tasinmazlar.Sorgu(takipEtme: false).Single(t => t.TasinmazNo == "T-IL2");
        Assert.Equal(TasinmazDurum.Aktif, il2Tasinmaz.Durum); // hâlâ aktif — YANLIŞLIKLA satıldı olmadı
    }

    [Fact]
    public async Task TamPortfoyModu_FarkliIlIlcedekiMevcutTasinmaziDaSatildiIsaretler()
    {
        // TamPortfoyMu=true ile eski/tam davranış korunuyor: dosyada bahsi geçmeyen HER aktif
        // taşınmaz (il/ilçe fark etmeksizin) "Satıldı" işaretlenir.
        var (servis, uow, kisiId) = await KurulumYapAsync();

        await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "il1_yukleme.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto>
            {
                new()
                {
                    TasinmazNo = "T-IL2", BagimsizBolumNo = 1, ZeminHisseId = "Z-IL2",
                    Nitelik = "Arsa", Il = "İl 2", Ilce = "İlçe 2", Mahalle = "Mahalle 2",
                    Ada = 200, Parsel = 10, Yuzolcum = 500
                }
            }
        }, "kullanici-1");

        var ikinciSonuc = await servis.OnaylaVeIsleAsync(new TasinmazOnayIstegiDto
        {
            KisiId = kisiId,
            KaynakDosyaAdi = "tam_portfoy.xlsx",
            KaynakTuru = "Excel",
            Satirlar = new List<MulkiyetAdayDto> { Aday("T-1", 4, "Z-01") },
            TamPortfoyMu = true // kullanıcı bu dosyanın TAM portföy olduğunu açıkça belirtiyor
        }, "kullanici-1");

        Assert.True(ikinciSonuc.TamPortfoyMu);
        Assert.Equal(1, ikinciSonuc.SatildiSayisi); // İl 2'deki taşınmaz da artık satıldı sayılır

        var il2Tasinmaz = uow.Tasinmazlar.Sorgu(takipEtme: false).Single(t => t.TasinmazNo == "T-IL2");
        Assert.Equal(TasinmazDurum.Satildi, il2Tasinmaz.Durum);
    }
}
