using ArsaTapu.Business.Tasinmaz;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Dto.Tasinmaz;
using ArsaTapu.Tests.TestYardimcilari;
using Xunit;

namespace ArsaTapu.Tests;

/// <summary>
/// Kullanıcı isteği üzerine eklendi: taşınmaz detayına bakarken, başka bir kişiyle ortak
/// (aynı BagimsizBolumNo + ZeminHisseId) veya komşu (aynı Ada/Parsel, farklı bölüm) olup
/// olmadığı GetirAsync yanıtında görünmeli.
/// </summary>
public class TasinmazServiceOrtaklikDetayTests
{
    private static async Task<(TasinmazService Servis, Kisi KisiA, Kisi KisiB, Kisi KisiC)> KurulumYapAsync()
    {
        var context = TestDbContextFactory.Olustur();

        var kisiA = new Kisi { AdSoyad = "Kişi A" };
        var kisiB = new Kisi { AdSoyad = "Kişi B" };
        var kisiC = new Kisi { AdSoyad = "Kişi C" };
        context.Kisiler.AddRange(kisiA, kisiB, kisiC);
        await context.SaveChangesAsync();

        // A + B: AYNI BagimsizBolumNo + ZeminHisseId -> gerçek ortaklık.
        context.Tasinmazlar.Add(new Tasinmaz
        {
            KisiId = kisiA.Id, Nitelik = "Arsa", Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1",
            Ada = 171, Parsel = 190, BagimsizBolumNo = 4, ZeminHisseId = "Z-04", Yuzolcum = 500,
            Durum = TasinmazDurum.Aktif
        });
        context.Tasinmazlar.Add(new Tasinmaz
        {
            KisiId = kisiB.Id, Nitelik = "Arsa", Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1",
            Ada = 171, Parsel = 190, BagimsizBolumNo = 4, ZeminHisseId = "Z-04", Yuzolcum = 500,
            Durum = TasinmazDurum.Aktif
        });
        // A + C: AYNI Ada/Parsel ama FARKLI BagimsizBolumNo/ZeminHisseId -> yalnızca komşuluk.
        context.Tasinmazlar.Add(new Tasinmaz
        {
            KisiId = kisiC.Id, Nitelik = "Arsa", Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1",
            Ada = 171, Parsel = 190, BagimsizBolumNo = 9, ZeminHisseId = "Z-09", Yuzolcum = 500,
            Durum = TasinmazDurum.Aktif
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context, new KisiRepository(context), new TasinmazRepository(context),
            new ParselKmlRepository(context), new YuklemeKaydiRepository(context));

        var servis = new TasinmazService(uow, new SahteYetkiKapsamService(), new MulkiyetTekillestirmeService(uow));

        return (servis, kisiA, kisiB, kisiC);
    }

    [Fact]
    public async Task Getir_OrtakTasinmazdaDigerKisiyiOrtakKisilerdeGosterir()
    {
        var (servis, kisiA, _, _) = await KurulumYapAsync();

        var aTasinmazi = (await servis.ListeleAsync(new TasinmazFiltreDto { KisiId = kisiA.Id })).Kayitlar.Single();
        var detay = await servis.GetirAsync(aTasinmazi.Id);

        Assert.Contains("Kişi B", detay.OrtakKisiler);
        Assert.DoesNotContain("Kişi A", detay.OrtakKisiler); // kendisi kendi ortağı sayılmaz
    }

    [Fact]
    public async Task Getir_KomsuTasinmazdaDigerKisiyiKomsuKisilerdeGosterirOrtakKisilerdeGostermez()
    {
        var (servis, kisiA, _, _) = await KurulumYapAsync();

        var aTasinmazi = (await servis.ListeleAsync(new TasinmazFiltreDto { KisiId = kisiA.Id })).Kayitlar.Single();
        var detay = await servis.GetirAsync(aTasinmazi.Id);

        Assert.Contains("Kişi C", detay.KomsuKisiler);
        Assert.DoesNotContain("Kişi C", detay.OrtakKisiler);
    }
}
