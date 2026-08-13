using ArsaTapu.Business.Ortaklik;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Tests.TestYardimcilari;
using Xunit;

namespace ArsaTapu.Tests;

/// <summary>
/// Frontend mockup'taki RAW_KAYITLAR ile BİREBİR AYNI 3 senaryo:
///   1. Saf ortaklık  — Ada 171/Parsel 190: Kişi A + Kişi B aynı BB/Hisse.
///   2. Karışık       — Ada 44/Parsel 12: Kişi A + Kişi B ortaklık, Kişi C farklı birimde (komşuluk).
///   3. Saf komşuluk  — Ada 302/Parsel 57: Kişi B ve Kişi D farklı birimlerde, ortaklık yok.
/// Bu, backend hesaplamasının frontend tablolarıyla birebir eşleştiğini doğrular.
/// </summary>
public class OrtaklikServiceTests
{
    private static async Task<IUnitOfWork> UcSenaryoluVeriSetiIleUnitOfWorkOlusturAsync()
    {
        var context = TestDbContextFactory.Olustur();

        var kisiA = new Kisi { AdSoyad = "Kişi A" };
        var kisiB = new Kisi { AdSoyad = "Kişi B" };
        var kisiC = new Kisi { AdSoyad = "Kişi C" };
        var kisiD = new Kisi { AdSoyad = "Kişi D" };
        context.Kisiler.AddRange(kisiA, kisiB, kisiC, kisiD);
        await context.SaveChangesAsync();

        Tasinmaz Kayit(int kisiId, string tasinmazNo, string il, string ilce, string mahalle,
            int ada, int parsel, int bbNo, string hisseId) => new()
        {
            KisiId = kisiId, TasinmazNo = tasinmazNo, Nitelik = "Arsa",
            Il = il, Ilce = ilce, Mahalle = mahalle, Ada = ada, Parsel = parsel,
            BagimsizBolumNo = bbNo, ZeminHisseId = hisseId, Yuzolcum = 500, Durum = TasinmazDurum.Aktif
        };

        // Senaryo 1: SAF ORTAKLIK — A + B aynı BB 4 / Hisse Z-01.
        context.Tasinmazlar.Add(Kayit(kisiA.Id, "T-1", "İl 1", "İlçe 2", "Mahalle 2", 171, 190, 4, "Z-01"));
        context.Tasinmazlar.Add(Kayit(kisiB.Id, "T-2", "İl 1", "İlçe 2", "Mahalle 2", 171, 190, 4, "Z-01"));

        // Senaryo 2: KARIŞIK — A + B aynı BB 2 / Hisse Z-10 (ortaklık); C farklı BB 6 / Hisse Z-11 (komşuluk).
        context.Tasinmazlar.Add(Kayit(kisiA.Id, "T-3", "İl 2", "İlçe 3", "Mahalle 3", 44, 12, 2, "Z-10"));
        context.Tasinmazlar.Add(Kayit(kisiB.Id, "T-4", "İl 2", "İlçe 3", "Mahalle 3", 44, 12, 2, "Z-10"));
        context.Tasinmazlar.Add(Kayit(kisiC.Id, "T-5", "İl 2", "İlçe 3", "Mahalle 3", 44, 12, 6, "Z-11"));

        // Senaryo 3: SAF KOMŞULUK — B ve D farklı BB/Hisse'lerde, ortaklık yok.
        context.Tasinmazlar.Add(Kayit(kisiB.Id, "T-6", "İl 3", "İlçe 4", "Mahalle 4", 302, 57, 9, "Z-20"));
        context.Tasinmazlar.Add(Kayit(kisiD.Id, "T-7", "İl 3", "İlçe 4", "Mahalle 4", 302, 57, 12, "Z-21"));

        await context.SaveChangesAsync();

        return new UnitOfWork(
            context,
            new KisiRepository(context),
            new TasinmazRepository(context),
            new ParselKmlRepository(context),
            new YuklemeKaydiRepository(context));
    }

    [Fact]
    public async Task GercekOrtaklik_SadeceSenaryo1VeSenaryo2ninOrtaklikBirimleriniDoner()
    {
        var uow = await UcSenaryoluVeriSetiIleUnitOfWorkOlusturAsync();
        var servis = new OrtaklikService(uow, new SahteYetkiKapsamService());

        var sonuc = await servis.GercekOrtaklikGetirAsync(null);

        // Yalnızca 2 gerçek ortaklık kaydı olmalı: Ada171/Parsel190 ve Ada44/Parsel12.
        // Ada302/Parsel57 (Senaryo 3) HİÇ görünmemeli — orada ortaklık yok, yalnızca komşuluk var.
        Assert.Equal(2, sonuc.Count);

        var senaryo1 = Assert.Single(sonuc, s => s.Ada == 171 && s.Parsel == 190);
        Assert.Equal(4, senaryo1.BagimsizBolumNo);
        Assert.Equal("Z-01", senaryo1.ZeminHisseId);
        Assert.Equal(new[] { "Kişi A", "Kişi B" }, senaryo1.OrtakKisiler.Select(k => k.AdSoyad).OrderBy(x => x));

        var senaryo2 = Assert.Single(sonuc, s => s.Ada == 44 && s.Parsel == 12);
        Assert.Equal(2, senaryo2.BagimsizBolumNo);
        Assert.Equal(new[] { "Kişi A", "Kişi B" }, senaryo2.OrtakKisiler.Select(k => k.AdSoyad).OrderBy(x => x));

        Assert.DoesNotContain(sonuc, s => s.Ada == 302);
    }

    [Fact]
    public async Task Komsuluk_SadeceSenaryo2VeSenaryo3unKomsulukParsellerini_Doner()
    {
        var uow = await UcSenaryoluVeriSetiIleUnitOfWorkOlusturAsync();
        var servis = new OrtaklikService(uow, new SahteYetkiKapsamService());

        var sonuc = await servis.KomsulukGetirAsync(null);

        // Yalnızca 2 komşuluk kaydı: Ada44/Parsel12 ve Ada302/Parsel57.
        // Ada171/Parsel190 (Senaryo 1, saf ortaklık) HİÇ görünmemeli.
        Assert.Equal(2, sonuc.Count);
        Assert.DoesNotContain(sonuc, s => s.Ada == 171);

        var senaryo2 = Assert.Single(sonuc, s => s.Ada == 44 && s.Parsel == 12);
        Assert.Equal(2, senaryo2.Birimler.Count);
        Assert.Contains(senaryo2.Birimler, b => b.BagimsizBolumNo == 2 && b.Kisiler.Count == 2); // A+B
        Assert.Contains(senaryo2.Birimler, b => b.BagimsizBolumNo == 6 && b.Kisiler.Single().AdSoyad == "Kişi C");

        var senaryo3 = Assert.Single(sonuc, s => s.Ada == 302 && s.Parsel == 57);
        Assert.Equal(2, senaryo3.Birimler.Count);
        Assert.Contains(senaryo3.Birimler, b => b.BagimsizBolumNo == 9 && b.Kisiler.Single().AdSoyad == "Kişi B");
        Assert.Contains(senaryo3.Birimler, b => b.BagimsizBolumNo == 12 && b.Kisiler.Single().AdSoyad == "Kişi D");
    }

    [Fact]
    public async Task KisiFiltresi_OrtaklikVeKomsulukSonuclariniFrontendVeAyniSemantiklaSinirlar()
    {
        var uow = await UcSenaryoluVeriSetiIleUnitOfWorkOlusturAsync();
        var kisiler = uow.Kisiler.Sorgu(takipEtme: false).OrderBy(k => k.AdSoyad).ToList();
        var kisiA = kisiler.Single(k => k.AdSoyad == "Kişi A").Id;
        var kisiB = kisiler.Single(k => k.AdSoyad == "Kişi B").Id;
        var kisiD = kisiler.Single(k => k.AdSoyad == "Kişi D").Id;

        var servis = new OrtaklikService(uow, new SahteYetkiKapsamService());

        // A+B filtresi: her iki ortaklık kaydı da (A,B içerdiği için) dönmeli.
        var ortaklikAB = await servis.GercekOrtaklikGetirAsync(new[] { kisiA, kisiB });
        Assert.Equal(2, ortaklikAB.Count);

        // B+D filtresi: yalnızca Ada302/Parsel57 (ikisini birden içeren tek parsel) dönmeli.
        var komsulukBD = await servis.KomsulukGetirAsync(new[] { kisiB, kisiD });
        Assert.Single(komsulukBD);
        Assert.Equal(302, komsulukBD[0].Ada);
    }
}
