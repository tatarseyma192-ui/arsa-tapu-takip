using ArsaTapu.Business.Common;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Tekillestirme;
using ArsaTapu.Dto.TasinmazYukleme;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.Business.TasinmazYukleme;

public class TasinmazYuklemeService : ITasinmazYuklemeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYetkiKapsamService _yetkiKapsam;
    private readonly IMulkiyetTekillestirmeService _mulkiyetTekillestirme;
    private readonly IKmlTekillestirmeService _kmlTekillestirme;
    private readonly IPdfSatirCikarici _pdfSatirCikarici;
    private readonly IExcelSatirCikarici _excelSatirCikarici;
    private readonly IExcelUreticiService _excelUretici;

    public TasinmazYuklemeService(
        IUnitOfWork unitOfWork,
        IYetkiKapsamService yetkiKapsam,
        IMulkiyetTekillestirmeService mulkiyetTekillestirme,
        IKmlTekillestirmeService kmlTekillestirme,
        IPdfSatirCikarici pdfSatirCikarici,
        IExcelSatirCikarici excelSatirCikarici,
        IExcelUreticiService excelUretici)
    {
        _unitOfWork = unitOfWork;
        _yetkiKapsam = yetkiKapsam;
        _mulkiyetTekillestirme = mulkiyetTekillestirme;
        _kmlTekillestirme = kmlTekillestirme;
        _pdfSatirCikarici = pdfSatirCikarici;
        _excelSatirCikarici = excelSatirCikarici;
        _excelUretici = excelUretici;
    }

    public async Task<TasinmazOnizlemeSonucuDto> PdfOnizlemeOlusturAsync(
        int kisiId, Stream pdfStream, string dosyaAdi, CancellationToken ct = default)
    {
        var hamSatirlar = _pdfSatirCikarici.SatirlariCikar(pdfStream);
        return await OnizlemeOlusturAsync(kisiId, hamSatirlar, dosyaAdi, "Pdf", ct);
    }

    public async Task<TasinmazOnizlemeSonucuDto> ExcelOnizlemeOlusturAsync(
        int kisiId, Stream excelStream, string dosyaAdi, CancellationToken ct = default)
    {
        var hamSatirlar = _excelSatirCikarici.SatirlariCikar(excelStream);
        return await OnizlemeOlusturAsync(kisiId, hamSatirlar, dosyaAdi, "Excel", ct);
    }

    private async Task<TasinmazOnizlemeSonucuDto> OnizlemeOlusturAsync(
        int kisiId, List<Dictionary<string, string?>> hamSatirlar, string dosyaAdi, string kaynakTuru,
        CancellationToken ct)
    {
        await _yetkiKapsam.KisiErisimKontrolEtAsync(kisiId, ct);

        var adaylar = new List<MulkiyetAdayDto>();
        var hatalar = new List<SatirHatasiDto>();

        for (var i = 0; i < hamSatirlar.Count; i++)
        {
            var satirNo = i + 1;
            var (aday, hata) = SatirDonusturucu.Donustur(hamSatirlar[i], satirNo);

            if (aday is not null)
                adaylar.Add(aday);
            else if (hata is not null)
                hatalar.Add(new SatirHatasiDto { SatirNo = satirNo, Mesaj = hata });
        }

        if (adaylar.Count == 0)
        {
            throw new BusinessRuleException(
                "Dosyadan hiç geçerli satır çıkarılamadı. Sütun başlıklarının ve veri biçiminin " +
                $"beklenen şemaya uygun olduğundan emin olun: {KanonikSutunlar.BeklenenBasliklarMetni()}");
        }

        var siniflandirma = await _mulkiyetTekillestirme.SiniflandirAsync(kisiId, adaylar, ct);

        var satirlar = new List<TasinmazOnizlemeSatiriDto>();
        var sayac = 1;

        foreach (var yeni in siniflandirma.YeniAlimlar)
            satirlar.Add(new TasinmazOnizlemeSatiriDto { SatirNo = sayac++, Aday = yeni, Durum = "YeniAlim" });

        foreach (var mevcut in siniflandirma.ZatenKayitliOlanlar)
            satirlar.Add(new TasinmazOnizlemeSatiriDto { SatirNo = sayac++, Aday = mevcut, Durum = "ZatenKayitli" });

        return new TasinmazOnizlemeSonucuDto
        {
            KaynakDosyaAdi = dosyaAdi,
            KaynakTuru = kaynakTuru,
            ToplamSatirSayisi = hamSatirlar.Count,
            GecerliSatirSayisi = adaylar.Count,
            YeniAlimSayisi = siniflandirma.YeniAlimlar.Count,
            ZatenKayitliSayisi = siniflandirma.ZatenKayitliOlanlar.Count,
            Satirlar = satirlar,
            SatirHatalari = hatalar
        };
    }

    public async Task<TasinmazOnaySonucuDto> OnaylaVeIsleAsync(
        TasinmazOnayIstegiDto istek, string yukleyenKullaniciId, CancellationToken ct = default)
    {
        await _yetkiKapsam.KisiErisimKontrolEtAsync(istek.KisiId, ct);

        if (!Enum.TryParse<KaynakTuru>(istek.KaynakTuru, true, out var kaynakTuru))
            throw new BusinessRuleException($"Geçersiz kaynak türü: '{istek.KaynakTuru}'. 'Pdf' veya 'Excel' olmalı.");

        if (istek.Satirlar.Count == 0)
            throw new BusinessRuleException("Onaylanacak en az bir satır olmalı.");

        // GÜVENLİK: istemcinin önizlemede gösterdiği "Yeni Alım / Zaten Kayıtlı" sınıflandırmasına
        // KÖRÜ KÖRÜNE güvenilmez — onay anında sunucu tarafında YENİDEN hesaplanır (Requirements
        // madde 4.1). Önizleme ile onay arasında başka bir yükleme araya girmiş olabilir.
        for (var i = 0; i < istek.Satirlar.Count; i++)
            GerekliAlanlariDogrula(istek.Satirlar[i], i + 1);

        var siniflandirma = await _mulkiyetTekillestirme.SiniflandirAsync(istek.KisiId, istek.Satirlar, ct);

        var yuklemeKaydi = new YuklemeKaydi
        {
            KisiId = istek.KisiId,
            YuklemeTarihi = DateTime.UtcNow,
            KaynakDosyaAdi = istek.KaynakDosyaAdi,
            KaynakTuru = kaynakTuru,
            YukleyenKullaniciId = yukleyenKullaniciId
        };
        await _unitOfWork.YuklemeKayitlari.EkleAsync(yuklemeKaydi, ct);

        // Yeni alımlar: Durum=Aktif, ilk/son görüldüğü yükleme bu yükleme (navigation ataması —
        // yuklemeKaydi henüz kaydedilmemiş/Id'si 0 olsa da EF Core tek SaveChanges içinde FK'yı
        // doğru şekilde çözer; iki ayrı SaveChanges'e bölünmez, işlem TEK transaction'da atomik kalır).
        foreach (var yeni in siniflandirma.YeniAlimlar)
        {
            var tasinmaz = new ArsaTapu.Domain.Entities.Tasinmaz
            {
                KisiId = istek.KisiId,
                TasinmazNo = yeni.TasinmazNo,
                Nitelik = yeni.Nitelik!,
                Il = yeni.Il!,
                Ilce = yeni.Ilce!,
                Mahalle = yeni.Mahalle!,
                Ada = yeni.Ada!.Value,
                Parsel = yeni.Parsel!.Value,
                BagimsizBolumNo = yeni.BagimsizBolumNo,
                ZeminHisseId = yeni.ZeminHisseId,
                Yuzolcum = yeni.Yuzolcum!.Value,
                Durum = TasinmazDurum.Aktif,
                IlkGorulduguYukleme = yuklemeKaydi,
                SonGorulduguYukleme = yuklemeKaydi
            };

            await _unitOfWork.Tasinmazlar.EkleAsync(tasinmaz, ct);
        }

        // Requirements madde 3: "Yeni Excel'de olup önceki yüklemede olmayan kayıt -> Yeni Alım",
        // "Önceki yüklemede olup yeni Excel'de olmayan kayıt -> Satıldı / Elden Çıktı (otomatik
        // silinmez, durumu değişir, geçmişte kalır)". Bunun için kişinin MEVCUT AKTİF taşınmazları
        // bu yüklemenin anahtar kümesiyle karşılaştırılır.
        var buYuklemedeGorulenAnahtarlar = siniflandirma.ZatenKayitliOlanlar
            .Select(z => (z.BagimsizBolumNo, z.ZeminHisseId))
            .ToHashSet();

        // KISMI YUKLEME DESTEGI (kullanıcı isteği): bazı yüklemeler kişinin TÜM portföyü değil,
        // yalnızca belirli bir il/ilçe içindir (ör. "sadece Gaziantep/Şahinbey"). TamPortfoyMu
        // false ise (VARSAYILAN, daha güvenli taraf), yalnızca dosyada GEÇEN il/ilçe kombinasyonları
        // "kapsam" sayılır — kapsam DIŞINDAKİ mevcut aktif taşınmazlara HİÇ DOKUNULMAZ. Böylece
        // kısmi bir dosya yüklendiğinde, dosyada bahsi hiç geçmeyen il/ilçelerdeki taşınmazlar
        // YANLIŞLIKLA "Satıldı" işaretlenmez.
        HashSet<(string Il, string Ilce)>? kapsamIlIlceler = null;
        if (!istek.TamPortfoyMu)
        {
            kapsamIlIlceler = istek.Satirlar
                .Where(s => !string.IsNullOrWhiteSpace(s.Il) && !string.IsNullOrWhiteSpace(s.Ilce))
                .Select(s => (Il: KapsamAnahtari(s.Il!), Ilce: KapsamAnahtari(s.Ilce!)))
                .ToHashSet();
        }

        var kisininMevcutAktifTasinmazlari = await _unitOfWork.Tasinmazlar.Sorgu(takipEtme: true)
            .Where(t => t.KisiId == istek.KisiId && t.Durum == TasinmazDurum.Aktif)
            .ToListAsync(ct);

        var satildiSayaci = 0;

        foreach (var mevcut in kisininMevcutAktifTasinmazlari)
        {
            if (kapsamIlIlceler is not null &&
                !kapsamIlIlceler.Contains((KapsamAnahtari(mevcut.Il), KapsamAnahtari(mevcut.Ilce))))
            {
                continue; // Bu yükleme bu il/ilçe hakkında hiçbir şey söylemiyor — dokunma.
            }

            var anahtar = (mevcut.BagimsizBolumNo, mevcut.ZeminHisseId);

            if (buYuklemedeGorulenAnahtarlar.Contains(anahtar))
            {
                // Hâlâ görüldü — son görülme bilgisini bu yüklemeye güncelle.
                mevcut.SonGorulduguYukleme = yuklemeKaydi;
                _unitOfWork.Tasinmazlar.Guncelle(mevcut);
            }
            else
            {
                // Bu yüklemede yok -> Satıldı. SonGorulduguYuklemeId BİLEREK değiştirilmez
                // (gerçekten son görüldüğü yükleme neredeyse o kalır; bu yükleme onu "görmedi").
                mevcut.Durum = TasinmazDurum.Satildi;
                _unitOfWork.Tasinmazlar.Guncelle(mevcut);
                satildiSayaci++;
            }
        }

        await _unitOfWork.KaydetAsync(ct);

        // KML tetikleme (Requirements madde 5): yalnızca TESPİT. TKGM sorgusu bu adımda YAPILMAZ.
        // Bu yüklemedeki TÜM satırların (yeni + zaten kayıtlı) parselleri değerlendirilir; KML
        // tekilleştirme anahtarı mülkiyet anahtarından bağımsızdır (madde 4.2).
        var parselAdaylari = istek.Satirlar
            .Select(s => new ParselAdayDto
            {
                Il = s.Il!, Ilce = s.Ilce!, Mahalle = s.Mahalle!, Ada = s.Ada!.Value, Parsel = s.Parsel!.Value
            })
            .ToList();

        var kmlSiniflandirma = await _kmlTekillestirme.SiniflandirAsync(parselAdaylari, ct);

        return new TasinmazOnaySonucuDto
        {
            YuklemeKaydiId = yuklemeKaydi.Id,
            KisiId = istek.KisiId,
            YuklemeTarihi = yuklemeKaydi.YuklemeTarihi,
            YeniAlimSayisi = siniflandirma.YeniAlimlar.Count,
            SatildiSayisi = satildiSayaci,
            ZatenKayitliSayisi = siniflandirma.ZatenKayitliOlanlar.Count,
            TamPortfoyMu = istek.TamPortfoyMu,
            DegerlendirilenIlIlceler = kapsamIlIlceler?
                .Select(k => $"{k.Il} / {k.Ilce}")
                .OrderBy(s => s)
                .ToList() ?? new List<string>(),
            KmlSorgulanmasiGerekenParseller = kmlSiniflandirma.SorgulanmasiGerekenler
        };
    }

    public byte[] OnizlemeyiExceleAktar(IReadOnlyList<MulkiyetAdayDto> satirlar) => _excelUretici.Uret(satirlar);

    /// <summary>
    /// Kısmi yükleme kapsamı karşılaştırmasında kullanılır — büyük/küçük harf farkına
    /// toleranslı olmak için basit bir normalize (Trim + ToUpperInvariant). TKGM eşleştirmesindeki
    /// (IlKodlari.Normallestir) kadar Türkçe-özel karakter işlemeye burada gerek yok, çünkü bu
    /// karşılaştırma AYNI parse pipeline'ından gelen iki değeri (mevcut kayıt vs yeni yükleme)
    /// kıyaslıyor — genelde zaten tutarlı geliyor, yalnızca büyük/küçük harf toleransı yeterli.
    /// </summary>
    private static string KapsamAnahtari(string deger) => deger.Trim().ToUpperInvariant();

    private static void GerekliAlanlariDogrula(MulkiyetAdayDto aday, int satirNo)
    {
        // NOT: TasinmazNo BİLEREK burada zorunlu tutulmaz — bazı kaynaklarda (ör. Excel,
        // 2026-08-04'te doğrulandı) bu sütun hiç yoktur ve tekilleştirme anahtarının
        // parçası değildir (bkz. IMulkiyetTekillestirmeService).
        if (string.IsNullOrWhiteSpace(aday.Nitelik))
            throw new BusinessRuleException($"{satirNo}. satır: Nitelik eksik.");
        if (string.IsNullOrWhiteSpace(aday.Il))
            throw new BusinessRuleException($"{satirNo}. satır: İl eksik.");
        if (string.IsNullOrWhiteSpace(aday.Ilce))
            throw new BusinessRuleException($"{satirNo}. satır: İlçe eksik.");
        if (string.IsNullOrWhiteSpace(aday.Mahalle))
            throw new BusinessRuleException($"{satirNo}. satır: Mahalle eksik.");
        if (string.IsNullOrWhiteSpace(aday.ZeminHisseId))
            throw new BusinessRuleException($"{satirNo}. satır: Zemin Hisse ID eksik.");
        if (!aday.Ada.HasValue)
            throw new BusinessRuleException($"{satirNo}. satır: Ada eksik.");
        if (!aday.Parsel.HasValue)
            throw new BusinessRuleException($"{satirNo}. satır: Parsel eksik.");
        if (!aday.Yuzolcum.HasValue)
            throw new BusinessRuleException($"{satirNo}. satır: Yüzölçüm eksik.");
    }
}
