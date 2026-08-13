using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.ParselKml;
using ArsaTapu.Dto.Tekillestirme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArsaTapu.Business.ParselSorgu;

public class ParselKmlService : IParselKmlService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKmlTekillestirmeService _kmlTekillestirme; // MEVCUT servis — yeniden yazılmadı
    private readonly IParselSorguIstemcisi _sorguIstemcisi;
    private readonly IKmlDosyaDepoService _dosyaDepo;
    private readonly bool _deneyselModu;

    public ParselKmlService(
        IUnitOfWork unitOfWork,
        IKmlTekillestirmeService kmlTekillestirme,
        IParselSorguIstemcisi sorguIstemcisi,
        IKmlDosyaDepoService dosyaDepo,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _kmlTekillestirme = kmlTekillestirme;
        _sorguIstemcisi = sorguIstemcisi;
        _dosyaDepo = dosyaDepo;

        // TKGM entegrasyonu artık gerçek/doğrulanmış API yapısına göre çalışıyor (2026-08-04
        // tarihli HAR kaydından çıkarıldı) — bu yüzden varsayılan olarak "deneysel" etiketi
        // KAPALIDIR. Gerçek kullanımda sorun görülürse appsettings'ten (ParselSorgu:DeneyselModu)
        // kod değişikliği olmadan tekrar açılabilir.
        _deneyselModu = configuration.GetValue<bool?>("ParselSorgu:DeneyselModu") ?? false;
    }

    public async Task<PagedResult<ParselKmlDto>> ListeleAsync(PagedRequest istek, CancellationToken ct = default)
    {
        var sorgu = _unitOfWork.ParselKmlleri.Sorgu(takipEtme: false);

        var toplam = await sorgu.CountAsync(ct);

        // NOT: DtoyaCevir burada bilerek IQueryable.Select içinde DEĞİL, materialize edildikten
        // SONRA (bellekte) çağrılır — EF Core keyfi bir C# metodunu SQL'e çeviremez.
        var entityler = await sorgu
            .OrderByDescending(x => x.CekilmeTarihi)
            .Skip((istek.Sayfa - 1) * istek.SayfaBoyutu)
            .Take(istek.SayfaBoyutu)
            .ToListAsync(ct);

        return new PagedResult<ParselKmlDto>
        {
            Kayitlar = entityler.Select(DtoyaCevir).ToList(),
            ToplamKayit = toplam,
            Sayfa = istek.Sayfa,
            SayfaBoyutu = istek.SayfaBoyutu
        };
    }

    public async Task<ParselSorguSonucuDto> SorgulaAsync(ParselSorguIstegiDto istek, CancellationToken ct = default)
    {
        // NOT: Hız sınırlayıcı artık burada DEĞİL, TkgmParselSorguIstemcisi içinde uygulanır —
        // tek bir SorgulaAsync çağrısı İÇİNDE birden fazla gerçek TKGM isteği (ilçe/mahalle
        // çözümleme + parsel doğrulama + KML indirme) olabileceğinden, doğru granülerlik
        // "her gerçek HTTP isteği öncesi" olmalı, "her iş operasyonu öncesi" değil.
        var sorguSonucu = await _sorguIstemcisi.SorgulaAsync(istek.Il, istek.Ilce, istek.Mahalle, istek.Ada, istek.Parsel, ct);

        var mevcutKayit = await _unitOfWork.ParselKmlleri.AnahtarIleBulAsync(
            istek.Il, istek.Ilce, istek.Mahalle, istek.Ada, istek.Parsel, ct);

        if (!sorguSonucu.Basarili)
        {
            await KaydiGuncelleVeyaOlusturAsync(mevcutKayit, istek, KmlDurum.Basarisiz, ParselKmlKaynagi.Otomatik, dosyaYolu: null, ct);

            return new ParselSorguSonucuDto
            {
                Il = istek.Il, Ilce = istek.Ilce, Mahalle = istek.Mahalle, Ada = istek.Ada, Parsel = istek.Parsel,
                Durum = "Basarisiz",
                HataMesaji = sorguSonucu.HataMesaji,
                Deneysel = false,
                DeneyselUyari = null
            };
        }

        var dosyaAdi = _dosyaDepo.DosyaAdiOlustur(istek.Il, istek.Ilce, istek.Mahalle, istek.Ada, istek.Parsel);
        var kmlIcerigi = KmlOlusturucu.AciklamaEnjekteEt(sorguSonucu.KmlBaytlari!, istek.TasinmazReferanslari);
        var dosyaYolu = await _dosyaDepo.KaydetAsync(dosyaAdi, kmlIcerigi, ct);

        await KaydiGuncelleVeyaOlusturAsync(mevcutKayit, istek, KmlDurum.Basarili, ParselKmlKaynagi.Otomatik, dosyaYolu, ct);

        return new ParselSorguSonucuDto
        {
            Il = istek.Il, Ilce = istek.Ilce, Mahalle = istek.Mahalle, Ada = istek.Ada, Parsel = istek.Parsel,
            Durum = "Basarili",
            DosyaYolu = dosyaYolu,
            Deneysel = _deneyselModu,
            DeneyselUyari = _deneyselModu ? "Doğrulanmadı, kontrol edin." : null
        };
    }

    public async Task<TopluParselSorguSonucuDto> TopluSorgulaAsync(TopluParselSorguIstegiDto istek, CancellationToken ct = default)
    {
        // Kullanıcı isteği: "her seferinde tüm listeyi yapmasın, opsiyon olsun — hepsi | belirli
        // tapular gibi". TumunuSecModu=true: kişinin KML'si eksik tüm aktif parselleri sunucu
        // tarafında hesaplanır. false (varsayılan): yalnızca istekte AÇIKÇA seçilen Parseller kullanılır.
        var parseller = istek.TumunuSecModu
            ? await KisininAktifParsellerimiOlusturAsync(
                istek.KisiId ?? throw new BusinessRuleException("TumunuSecModu=true iken KisiId zorunludur."), ct)
            : istek.Parseller;

        if (parseller.Count == 0)
            throw new BusinessRuleException("Sorgulanacak en az bir parsel olmalı.");

        // MEVCUT IKmlTekillestirmeService aynen kullanılıyor: zaten başarıyla çekilmiş
        // parseller tekrar sorgulanmaz (Requirements madde 4.2 — gereksiz TKGM yükü önlenir).
        var adaylar = parseller
            .Select(p => new ParselAdayDto { Il = p.Il, Ilce = p.Ilce, Mahalle = p.Mahalle, Ada = p.Ada, Parsel = p.Parsel })
            .ToList();

        var siniflandirma = await _kmlTekillestirme.SiniflandirAsync(adaylar, ct);

        var sonuclar = new List<ParselSorguSonucuDto>();

        foreach (var parsel in siniflandirma.SorgulanmasiGerekenler)
        {
            var orijinalIstek = parseller.First(p =>
                p.Il == parsel.Il && p.Ilce == parsel.Ilce && p.Mahalle == parsel.Mahalle &&
                p.Ada == parsel.Ada && p.Parsel == parsel.Parsel);

            var sonuc = await SorgulaAsync(orijinalIstek, ct); // hız sınırlayıcı istemci içinde, her gerçek çağrıda uygulanır
            sonuclar.Add(sonuc);
        }

        return new TopluParselSorguSonucuDto
        {
            ToplamSorgu = sonuclar.Count,
            BasariliSayisi = sonuclar.Count(s => s.Durum == "Basarili"),
            BasarisizSayisi = sonuclar.Count(s => s.Durum == "Basarisiz"),
            AtlananSayisi = siniflandirma.ZatenCekilmisOlanlar.Count,
            Sonuclar = sonuclar
        };
    }

    /// <summary>
    /// "Tümünü seç" modu: kişinin AKTİF taşınmazlarından (Satıldı olanlar hariç — onlar için KML
    /// gerekmez) benzersiz Ada/Parsel kombinasyonlarını çıkarır. Aynı Ada/Parsel'e bağlı birden
    /// fazla Bağımsız Bölüm varsa (Requirements madde 4.2), hepsi TasinmazReferanslari'nde
    /// toplanır — KML description'a doğru şekilde yansısın diye.
    /// </summary>
    private async Task<List<ParselSorguIstegiDto>> KisininAktifParsellerimiOlusturAsync(int kisiId, CancellationToken ct)
    {
        var aktifTasinmazlar = await _unitOfWork.Tasinmazlar.Sorgu(takipEtme: false)
            .Where(t => t.KisiId == kisiId && t.Durum == TasinmazDurum.Aktif)
            .ToListAsync(ct);

        return aktifTasinmazlar
            .GroupBy(t => (t.Il, t.Ilce, t.Mahalle, t.Ada, t.Parsel))
            .Select(grup => new ParselSorguIstegiDto
            {
                Il = grup.Key.Il,
                Ilce = grup.Key.Ilce,
                Mahalle = grup.Key.Mahalle,
                Ada = grup.Key.Ada,
                Parsel = grup.Key.Parsel,
                TasinmazReferanslari = grup
                    .Select(t => new TasinmazReferansDto { TasinmazNo = t.TasinmazNo, BagimsizBolumNo = t.BagimsizBolumNo })
                    .ToList()
            })
            .ToList();
    }

    /// <summary>
    /// BİRİNCİL/GÜVENİLİR yol: kullanıcı kendi indirdiği KML dosyasını yükler. TKGM istemcisine
    /// hiç gidilmez (Requirements madde 5: "kendi indirdiği KML'i manuel yükleyebilir").
    /// </summary>
    public async Task<ParselKmlDto> ManuelYukleAsync(
        ParselSorguIstegiDto parselBilgisi, byte[] kmlIcerigi, CancellationToken ct = default)
    {
        var dosyaAdi = _dosyaDepo.DosyaAdiOlustur(
            parselBilgisi.Il, parselBilgisi.Ilce, parselBilgisi.Mahalle, parselBilgisi.Ada, parselBilgisi.Parsel);

        var dosyaYolu = await _dosyaDepo.KaydetAsync(dosyaAdi, kmlIcerigi, ct);

        var mevcutKayit = await _unitOfWork.ParselKmlleri.AnahtarIleBulAsync(
            parselBilgisi.Il, parselBilgisi.Ilce, parselBilgisi.Mahalle, parselBilgisi.Ada, parselBilgisi.Parsel, ct);

        var guncellenenKayit = await KaydiGuncelleVeyaOlusturAsync(
            mevcutKayit, parselBilgisi, KmlDurum.Basarili, ParselKmlKaynagi.Manuel, dosyaYolu, ct);

        return DtoyaCevir(guncellenenKayit);
    }

    public async Task SilAsync(int id, CancellationToken ct = default)
    {
        var kayit = await _unitOfWork.ParselKmlleri.GetirAsync(id, ct)
            ?? throw new NotFoundException("ParselKml", id);

        var dosyaYolu = kayit.DosyaYolu;

        // Soft delete (Handbook madde 5) — global sorgu filtresi sayesinde bu kayıt artık
        // IKmlTekillestirmeService.BasariliAnahtarlariGetirAsync() sonucunda GÖRÜNMEZ; yani
        // aynı Ada/Parsel bir sonraki sınıflandırmada otomatik olarak "sorgulanması gereken"
        // listesine geri döner (Requirements madde 4.3) — ayrıca bir kod değişikliği gerekmez.
        _unitOfWork.ParselKmlleri.Sil(kayit);
        await _unitOfWork.KaydetAsync(ct);

        if (!string.IsNullOrWhiteSpace(dosyaYolu))
            await _dosyaDepo.SilAsync(dosyaYolu, ct);
    }

    private async Task<ParselKml> KaydiGuncelleVeyaOlusturAsync(
        ParselKml? mevcut, ParselSorguIstegiDto istek, KmlDurum durum, ParselKmlKaynagi kaynak, string? dosyaYolu,
        CancellationToken ct)
    {
        if (mevcut is not null)
        {
            mevcut.Durum = durum;
            mevcut.Kaynak = kaynak;
            mevcut.DosyaYolu = dosyaYolu;
            mevcut.CekilmeTarihi = DateTime.UtcNow;
            _unitOfWork.ParselKmlleri.Guncelle(mevcut);
        }
        else
        {
            mevcut = new ParselKml
            {
                Il = istek.Il,
                Ilce = istek.Ilce,
                Mahalle = istek.Mahalle,
                Ada = istek.Ada,
                Parsel = istek.Parsel,
                Durum = durum,
                Kaynak = kaynak,
                DosyaYolu = dosyaYolu,
                CekilmeTarihi = DateTime.UtcNow
            };
            await _unitOfWork.ParselKmlleri.EkleAsync(mevcut, ct);
        }

        await _unitOfWork.KaydetAsync(ct);
        return mevcut;
    }

    private ParselKmlDto DtoyaCevir(ParselKml kayit)
    {
        var deneysel = kayit.Kaynak == ParselKmlKaynagi.Otomatik && _deneyselModu;

        return new ParselKmlDto
        {
            Id = kayit.Id,
            Il = kayit.Il,
            Ilce = kayit.Ilce,
            Mahalle = kayit.Mahalle,
            Ada = kayit.Ada,
            Parsel = kayit.Parsel,
            DosyaYolu = kayit.DosyaYolu,
            CekilmeTarihi = kayit.CekilmeTarihi,
            Durum = kayit.Durum.ToString(),
            Kaynak = kayit.Kaynak.ToString(),
            Deneysel = deneysel,
            DeneyselUyari = deneysel ? "Doğrulanmadı, kontrol edin." : null
        };
    }
}
