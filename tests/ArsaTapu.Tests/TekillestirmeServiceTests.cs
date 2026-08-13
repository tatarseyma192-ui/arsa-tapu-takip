using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Dto.Tekillestirme;
using ArsaTapu.Tests.TestYardimcilari;
using Xunit;

namespace ArsaTapu.Tests;

/// <summary>
/// İki tekilleştirme anahtarının (mülkiyet vs KML) birbirine karışmadığını doğrular.
/// </summary>
public class TekillestirmeServiceTests
{
    [Fact]
    public async Task MulkiyetTekillestirme_AyniAnahtarZatenKayitliOlarakIsaretlenir()
    {
        var context = TestDbContextFactory.Olustur();
        var kisi = new Kisi { AdSoyad = "Kişi A" };
        context.Kisiler.Add(kisi);
        await context.SaveChangesAsync();

        context.Tasinmazlar.Add(new Tasinmaz
        {
            KisiId = kisi.Id, TasinmazNo = "T-1", Nitelik = "Arsa",
            Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1", Ada = 1, Parsel = 1,
            BagimsizBolumNo = 2, ZeminHisseId = "Z-01", Yuzolcum = 100, Durum = TasinmazDurum.Aktif
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context, new KisiRepository(context), new TasinmazRepository(context),
            new ParselKmlRepository(context), new YuklemeKaydiRepository(context));
        var servis = new MulkiyetTekillestirmeService(uow);

        var adaylar = new List<MulkiyetAdayDto>
        {
            new() { TasinmazNo = "T-1", BagimsizBolumNo = 2, ZeminHisseId = "Z-01" }, // zaten kayıtlı
            new() { TasinmazNo = "T-2", BagimsizBolumNo = 3, ZeminHisseId = "Z-02" }  // yeni alım
        };

        var sonuc = await servis.SiniflandirAsync(kisi.Id, adaylar);

        Assert.Single(sonuc.ZatenKayitliOlanlar);
        Assert.Single(sonuc.YeniAlimlar);
        Assert.Equal("T-2", sonuc.YeniAlimlar[0].TasinmazNo);
    }

    [Fact]
    public async Task MulkiyetTekillestirme_TasinmazNoFarkliVeyaYokOlsaDaAnahtarEslesir()
    {
        // 2026-08-04'te doğrulanan gerçek senaryo: aynı gerçek taşınmaz PDF'ten (TasinmazNo'lu)
        // VE Excel'den (TasinmazNo'suz) yüklenebiliyor. TasinmazNo anahtarın PARÇASI OLMADIĞI
        // için, bu iki kaynak aynı BagimsizBolumNo+ZeminHisseId ile geldiğinde AYNI taşınmaz
        // olarak tanınmalı — TasinmazNo hiç yoksa bile.
        var context = TestDbContextFactory.Olustur();
        var kisi = new Kisi { AdSoyad = "Kişi A" };
        context.Kisiler.Add(kisi);
        await context.SaveChangesAsync();

        // Mevcut kayıt PDF'ten geldi — TasinmazNo dolu.
        context.Tasinmazlar.Add(new Tasinmaz
        {
            KisiId = kisi.Id, TasinmazNo = "8027710", Nitelik = "Mesken",
            Il = "Mersin", Ilce = "Erdemli", Mahalle = "Çeşmeli", Ada = 121, Parsel = 1,
            BagimsizBolumNo = 18, ZeminHisseId = "17827152", Yuzolcum = 31266, Durum = TasinmazDurum.Aktif
        });
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context, new KisiRepository(context), new TasinmazRepository(context),
            new ParselKmlRepository(context), new YuklemeKaydiRepository(context));
        var servis = new MulkiyetTekillestirmeService(uow);

        // Excel'den gelen aday — TasinmazNo HİÇ YOK (null), ama aynı BagimsizBolumNo+ZeminHisseId.
        var adaylar = new List<MulkiyetAdayDto>
        {
            new() { TasinmazNo = null, BagimsizBolumNo = 18, ZeminHisseId = "17827152" }
        };

        var sonuc = await servis.SiniflandirAsync(kisi.Id, adaylar);

        Assert.Single(sonuc.ZatenKayitliOlanlar); // "Yeni Alım" değil — aynı taşınmaz tanınmalı
        Assert.Empty(sonuc.YeniAlimlar);
    }

    [Fact]
    public async Task KmlTekillestirme_AyniAdaParselBirdenFazlaGelsedeTekSorguListesineEklenir()
    {
        var context = TestDbContextFactory.Olustur();
        var uow = new UnitOfWork(context, new KisiRepository(context), new TasinmazRepository(context),
            new ParselKmlRepository(context), new YuklemeKaydiRepository(context));
        var servis = new KmlTekillestirmeService(uow);

        var adaylar = new List<ParselAdayDto>
        {
            new() { Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1", Ada = 5, Parsel = 10 },
            new() { Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1", Ada = 5, Parsel = 10 }, // ayni parsel, tekrar
            new() { Il = "İl 1", Ilce = "İlçe 1", Mahalle = "Mahalle 1", Ada = 6, Parsel = 11 }
        };

        var sonuc = await servis.SiniflandirAsync(adaylar);

        Assert.Equal(2, sonuc.SorgulanmasiGerekenler.Count);
        Assert.Empty(sonuc.ZatenCekilmisOlanlar);
    }
}
