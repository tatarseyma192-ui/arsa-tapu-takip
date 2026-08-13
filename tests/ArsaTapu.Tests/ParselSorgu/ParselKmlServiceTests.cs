using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.ParselKml;
using ArsaTapu.Tests.TestYardimcilari;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArsaTapu.Tests.ParselSorgu;

/// <summary>
/// Requirements madde 5 + 4.2 + 4.3: sorgu/hata toleransı/manuel yükleme/silme sonrası
/// yeniden sorgulanabilirlik. Mevcut IKmlTekillestirmeService yeniden yazılmadan, aynen
/// kullanılarak test edilir. TKGM entegrasyonu gerçek HAR kaydıyla doğrulandığı için
/// varsayılan DeneyselModu artık false'tur (ayrı bir testte true durumu da doğrulanır).
/// </summary>
public class ParselKmlServiceTests
{
    private static (ParselKmlService Servis, IUnitOfWork UnitOfWork, SahteParselSorguIstemcisi Istemci, SahteKmlDosyaDepoService DosyaDepo)
        KurulumYap(Func<string, string, string, int, int, ParselSorguIstemciSonucu>? istemciDavranisi = null, bool deneyselModu = false)
    {
        var context = TestDbContextFactory.Olustur();

        var uow = new UnitOfWork(
            context,
            new KisiRepository(context),
            new TasinmazRepository(context),
            new ParselKmlRepository(context),
            new YuklemeKaydiRepository(context));

        var kmlTekillestirme = new KmlTekillestirmeService(uow); // MEVCUT servis, aynen kullanılıyor
        var istemci = new SahteParselSorguIstemcisi(istemciDavranisi);
        var dosyaDepo = new SahteKmlDosyaDepoService();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ParselSorgu:DeneyselModu"] = deneyselModu ? "true" : "false"
            })
            .Build();

        var servis = new ParselKmlService(uow, kmlTekillestirme, istemci, dosyaDepo, config);

        return (servis, uow, istemci, dosyaDepo);
    }

    private static ParselSorguIstegiDto OrnekIstek(string parsel = "190") => new()
    {
        Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1", Ada = 171, Parsel = int.Parse(parsel),
        TasinmazReferanslari = new List<TasinmazReferansDto> { new() { TasinmazNo = "T-1", BagimsizBolumNo = 4 } }
    };

    [Fact]
    public async Task Sorgula_BasariliOlunca_BasariliKayitVeDosyaOlusturur()
    {
        var (servis, uow, _, dosyaDepo) = KurulumYap();

        var sonuc = await servis.SorgulaAsync(OrnekIstek());

        Assert.Equal("Basarili", sonuc.Durum);
        Assert.NotNull(sonuc.DosyaYolu);
        Assert.Single(dosyaDepo.Dosyalar);

        // TKGM entegrasyonu gerçek HAR kaydıyla doğrulandı — varsayılan DeneyselModu=false
        // olduğunda artık "Doğrulanmadı" uyarısı gösterilmez.
        Assert.False(sonuc.Deneysel);
        Assert.Null(sonuc.DeneyselUyari);

        var kayitlar = uow.ParselKmlleri.Sorgu(takipEtme: false).ToList();
        Assert.Single(kayitlar);
        Assert.Equal(KmlDurum.Basarili, kayitlar[0].Durum);
        Assert.Equal(ParselKmlKaynagi.Otomatik, kayitlar[0].Kaynak);
    }

    [Fact]
    public async Task Sorgula_DeneyselModuAcikken_UyariGosterilir()
    {
        var (servis, _, _, _) = KurulumYap(deneyselModu: true);

        var sonuc = await servis.SorgulaAsync(OrnekIstek());

        // appsettings üzerinden (kod değişikliği olmadan) tekrar açılabilme senaryosu.
        Assert.True(sonuc.Deneysel);
        Assert.Equal("Doğrulanmadı, kontrol edin.", sonuc.DeneyselUyari);
    }

    [Fact]
    public async Task Sorgula_BasarisizOlunca_BasarisizKayitOlusturur_DosyaYok()
    {
        var (servis, uow, _, dosyaDepo) = KurulumYap((_, _, _, _, _) =>
            ParselSorguIstemciSonucu.BasarisizSonuc("TKGM parsel bulamadı."));

        var sonuc = await servis.SorgulaAsync(OrnekIstek());

        Assert.Equal("Basarisiz", sonuc.Durum);
        Assert.Null(sonuc.DosyaYolu);
        Assert.NotNull(sonuc.HataMesaji);
        Assert.Empty(dosyaDepo.Dosyalar);

        var kayitlar = uow.ParselKmlleri.Sorgu(takipEtme: false).ToList();
        Assert.Single(kayitlar);
        Assert.Equal(KmlDurum.Basarisiz, kayitlar[0].Durum);
    }

    [Fact]
    public async Task Sorgula_TekrarDeneme_YeniSatirOlusturmazMevcuduGunceller()
    {
        var basarisizMi = true;
        var (servis, uow, _, _) = KurulumYap((_, _, _, _, _) =>
            basarisizMi
                ? ParselSorguIstemciSonucu.BasarisizSonuc("Geçici hata.")
                : ParselSorguIstemciSonucu.BasariliSonuc(
                    System.Text.Encoding.UTF8.GetBytes(SahteParselSorguIstemcisi.OrnekKml)));

        var ilkSonuc = await servis.SorgulaAsync(OrnekIstek());
        Assert.Equal("Basarisiz", ilkSonuc.Durum);

        basarisizMi = false; // kullanıcı "Tekrar dene" der, bu sefer TKGM başarılı döner
        var ikinciSonuc = await servis.SorgulaAsync(OrnekIstek());
        Assert.Equal("Basarili", ikinciSonuc.Durum);

        // Aynı anahtar için TEK kayıt olmalı (yeni satır değil, güncellenmiş satır) —
        // aksi halde unique index çakışması gerçek bir PostgreSQL'de hata verirdi.
        var kayitlar = uow.ParselKmlleri.Sorgu(takipEtme: false).ToList();
        Assert.Single(kayitlar);
        Assert.Equal(KmlDurum.Basarili, kayitlar[0].Durum);
    }

    [Fact]
    public async Task Sil_SonraTekrarSorgulama_YeniKayitOlusturulabilir()
    {
        var (servis, uow, _, _) = KurulumYap();

        var ilkSonuc = await servis.SorgulaAsync(OrnekIstek());
        Assert.Equal("Basarili", ilkSonuc.Durum);

        var kayitOncesi = uow.ParselKmlleri.Sorgu(takipEtme: false).Single();
        await servis.SilAsync(kayitOncesi.Id);

        // Requirements madde 4.3: silindikten sonra aynı Ada/Parsel tekrar sorgulanabilir olmalı
        // (unique index artık yalnızca silinmemiş kayıtlar arasında zorlanıyor).
        var ikinciSonuc = await servis.SorgulaAsync(OrnekIstek());
        Assert.Equal("Basarili", ikinciSonuc.Durum);

        var aktifKayitlar = uow.ParselKmlleri.Sorgu(takipEtme: false).ToList();
        Assert.Single(aktifKayitlar); // eski silinmiş kayıt sorguda görünmez (global filtre)
    }

    [Fact]
    public async Task TopluSorgula_ZatenBasariylaCekilmisOlaniAtlar()
    {
        var (servis, _, istemci, _) = KurulumYap();

        // Önce bir parseli başarıyla çek.
        await servis.SorgulaAsync(OrnekIstek("190"));
        Assert.Equal(1, istemci.CagriSayisi);

        // Toplu sorguda hem zaten çekilmiş (190) hem yeni (191) parsel gönderiliyor.
        var topluIstek = new TopluParselSorguIstegiDto
        {
            Parseller = new List<ParselSorguIstegiDto> { OrnekIstek("190"), OrnekIstek("191") }
        };

        var sonuc = await servis.TopluSorgulaAsync(topluIstek);

        Assert.Equal(1, sonuc.AtlananSayisi); // 190 atlandı
        Assert.Equal(1, sonuc.ToplamSorgu);   // yalnızca 191 sorgulandı
        Assert.Equal(2, istemci.CagriSayisi); // toplamda: ilk çağrı + bu turda yalnızca 191 için 1 çağrı
    }

    [Fact]
    public async Task ManuelYukle_TkgmIstemcisiniHicCagirmaz()
    {
        var (servis, uow, istemci, dosyaDepo) = KurulumYap();

        var kmlIcerigi = System.Text.Encoding.UTF8.GetBytes("<kml>kullanıcının kendi dosyası</kml>");
        var sonuc = await servis.ManuelYukleAsync(OrnekIstek(), kmlIcerigi);

        Assert.Equal("Basarili", sonuc.Durum);
        Assert.Equal(0, istemci.CagriSayisi); // TKGM'e hiç gidilmedi
        Assert.Single(dosyaDepo.Dosyalar);

        // Manuel yol birincil/güvenilir kabul edilir: Deneysel=false, uyarı yok.
        Assert.False(sonuc.Deneysel);
        Assert.Null(sonuc.DeneyselUyari);
        Assert.Equal("Manuel", sonuc.Kaynak);

        var kayitlar = uow.ParselKmlleri.Sorgu(takipEtme: false).ToList();
        Assert.Single(kayitlar);
        Assert.Equal(KmlDurum.Basarili, kayitlar[0].Durum);
        Assert.Equal(ParselKmlKaynagi.Manuel, kayitlar[0].Kaynak);
    }

    [Fact]
    public async Task Listele_OtomatikVeManuelKayitlarDogruIsaretlenir()
    {
        var (servis, _, _, _) = KurulumYap(deneyselModu: true); // deneysel modu açık senaryoda ayrım net görünsün

        await servis.SorgulaAsync(OrnekIstek("190")); // otomatik -> deneysel modu açıkken uyarılı
        await servis.ManuelYukleAsync(OrnekIstek("191"), System.Text.Encoding.UTF8.GetBytes("<kml/>")); // manuel -> her zaman güvenilir

        var sonuc = await servis.ListeleAsync(new PagedRequest());

        var otomatikKayit = sonuc.Kayitlar.Single(k => k.Parsel == 190);
        var manuelKayit = sonuc.Kayitlar.Single(k => k.Parsel == 191);

        Assert.True(otomatikKayit.Deneysel);
        Assert.Equal("Otomatik", otomatikKayit.Kaynak);

        Assert.False(manuelKayit.Deneysel);
        Assert.Equal("Manuel", manuelKayit.Kaynak);
    }

    [Fact]
    public async Task TopluSorgula_TumunuSecModu_KisininAktifParsellerimiOtomatikBulur()
    {
        // Kullanıcı isteği: "kml taraması her seferinde tüm listeyi yapmasın, opsiyon olsun —
        // hepsi | belirli tapular gibi." TumunuSecModu=true iken Parseller listesi hiç
        // gönderilmeden, kişinin aktif taşınmazlarından ada/parsel listesi OTOMATİK çıkarılmalı.
        var (servis, uow, istemci, _) = KurulumYap();

        var kisi = new Kisi { AdSoyad = "Kişi A" };
        await uow.Kisiler.EkleAsync(kisi);
        await uow.KaydetAsync();

        await uow.Tasinmazlar.EkleAsync(new Tasinmaz
        {
            KisiId = kisi.Id, Nitelik = "Arsa", Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1",
            Ada = 100, Parsel = 5, ZeminHisseId = "Z-01", Yuzolcum = 500, Durum = TasinmazDurum.Aktif
        });
        await uow.Tasinmazlar.EkleAsync(new Tasinmaz
        {
            KisiId = kisi.Id, Nitelik = "Arsa", Il = "İl 2", Ilce = "İlçe 2", Mahalle = "Mahalle 2",
            Ada = 200, Parsel = 8, ZeminHisseId = "Z-02", Yuzolcum = 700, Durum = TasinmazDurum.Aktif
        });
        // Satılmış (aktif olmayan) bir taşınmaz — KML'ye ihtiyacı yok, kapsam dışı kalmalı.
        await uow.Tasinmazlar.EkleAsync(new Tasinmaz
        {
            KisiId = kisi.Id, Nitelik = "Arsa", Il = "İl 3", Ilce = "İlçe 3", Mahalle = "Mahalle 3",
            Ada = 300, Parsel = 9, ZeminHisseId = "Z-03", Yuzolcum = 300, Durum = TasinmazDurum.Satildi
        });
        await uow.KaydetAsync();

        var sonuc = await servis.TopluSorgulaAsync(new TopluParselSorguIstegiDto
        {
            TumunuSecModu = true,
            KisiId = kisi.Id
        });

        Assert.Equal(2, sonuc.ToplamSorgu); // yalnızca 2 aktif parsel, satılmış olan dahil değil
        Assert.Equal(2, istemci.CagriSayisi);
    }
}
