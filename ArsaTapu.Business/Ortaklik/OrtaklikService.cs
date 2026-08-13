using ArsaTapu.Business.Common;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Dto.Kisi;
using ArsaTapu.Dto.Ortaklik;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.Business.Ortaklik;

/// <summary>
/// Frontend'deki "Gerçek ortaklık" / "Komşu mülk sahipleri" ayrımıyla birebir aynı
/// mantığı backend'de uygular:
///   - Ortaklık anahtarı: Il+Ilce+Mahalle+Ada+Parsel+BagimsizBolumNo+ZeminHisseId (>1 farklı kişi).
///   - Komşuluk: Il+Ilce+Mahalle+Ada+Parsel aynı olup birden fazla farklı
///     (BagimsizBolumNo, ZeminHisseId) biriminin bulunduğu parseller.
/// Yalnızca Aktif taşınmazlar dikkate alınır (satılmış kayıtlar güncel ortaklığı yansıtmaz).
/// </summary>
public class OrtaklikService : IOrtaklikService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYetkiKapsamService _yetkiKapsam;

    public OrtaklikService(IUnitOfWork unitOfWork, IYetkiKapsamService yetkiKapsam)
    {
        _unitOfWork = unitOfWork;
        _yetkiKapsam = yetkiKapsam;
    }

    private sealed record HamKayit(
        int KisiId, string AdSoyad, string Il, string Ilce, string Mahalle,
        int Ada, int Parsel, int? BagimsizBolumNo, string ZeminHisseId);

    private async Task<List<HamKayit>> HamKayitlariGetirAsync(CancellationToken ct)
    {
        return await _unitOfWork.Tasinmazlar.Sorgu(takipEtme: false)
            .Where(x => x.Durum == TasinmazDurum.Aktif)
            .Select(x => new HamKayit(
                x.KisiId,
                x.Kisi!.AdSoyad,
                x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel,
                x.BagimsizBolumNo, x.ZeminHisseId))
            .ToListAsync(ct);
    }

    /// <summary>Patron ise filtreyi kendi KisiId'sine sabitler (yalnızca kendi payının göründüğü kayıtlar).</summary>
    private async Task<int[]?> EtkinFiltreyiHesaplaAsync(int[]? kisiIds, CancellationToken ct)
    {
        var patronKisiId = await _yetkiKapsam.PatronKisiIdGetirAsync(ct);
        if (patronKisiId.HasValue) return new[] { patronKisiId.Value };

        return kisiIds is { Length: > 0 } ? kisiIds : null;
    }

    public async Task<IReadOnlyList<GercekOrtaklikDto>> GercekOrtaklikGetirAsync(int[]? kisiIds, CancellationToken ct = default)
    {
        var etkinFiltre = await EtkinFiltreyiHesaplaAsync(kisiIds, ct);
        var kayitlar = await HamKayitlariGetirAsync(ct);

        var sonuc = kayitlar
            .GroupBy(x => (x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel, x.BagimsizBolumNo, x.ZeminHisseId))
            .Where(g => g.Select(x => x.KisiId).Distinct().Count() > 1)
            .Select(g => new GercekOrtaklikDto
            {
                Il = g.Key.Il,
                Ilce = g.Key.Ilce,
                Mahalle = g.Key.Mahalle,
                Ada = g.Key.Ada,
                Parsel = g.Key.Parsel,
                BagimsizBolumNo = g.Key.BagimsizBolumNo,
                ZeminHisseId = g.Key.ZeminHisseId,
                OrtakKisiler = g.Select(x => new KisiKisaDto { Id = x.KisiId, AdSoyad = x.AdSoyad })
                    .DistinctBy(k => k.Id)
                    .ToList()
            })
            .Where(u => etkinFiltre is null || etkinFiltre.All(id => u.OrtakKisiler.Any(k => k.Id == id)))
            .OrderBy(u => u.Il).ThenBy(u => u.Ada).ThenBy(u => u.Parsel)
            .ToList();

        return sonuc;
    }

    public async Task<IReadOnlyList<KomsulukDto>> KomsulukGetirAsync(int[]? kisiIds, CancellationToken ct = default)
    {
        var etkinFiltre = await EtkinFiltreyiHesaplaAsync(kisiIds, ct);
        var kayitlar = await HamKayitlariGetirAsync(ct);

        var sonuc = new List<KomsulukDto>();

        foreach (var parselGrubu in kayitlar.GroupBy(x => (x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel)))
        {
            var birimler = parselGrubu
                .GroupBy(x => (x.BagimsizBolumNo, x.ZeminHisseId))
                .Select(bg => new KomsulukBirimDto
                {
                    BagimsizBolumNo = bg.Key.BagimsizBolumNo,
                    ZeminHisseId = bg.Key.ZeminHisseId,
                    Kisiler = bg.Select(x => new KisiKisaDto { Id = x.KisiId, AdSoyad = x.AdSoyad })
                        .DistinctBy(k => k.Id)
                        .ToList()
                })
                .ToList();

            // Tek birim varsa (herkes aynı bağımsız bölüm/hisseyi paylaşıyorsa) bu saf ortaklıktır,
            // komşuluk değildir — burada listelenmez.
            if (birimler.Count <= 1) continue;

            var parseldekiTumKisiIdler = birimler
                .SelectMany(b => b.Kisiler)
                .Select(k => k.Id)
                .Distinct()
                .ToHashSet();

            if (etkinFiltre is not null && !etkinFiltre.All(parseldekiTumKisiIdler.Contains)) continue;

            sonuc.Add(new KomsulukDto
            {
                Il = parselGrubu.Key.Il,
                Ilce = parselGrubu.Key.Ilce,
                Mahalle = parselGrubu.Key.Mahalle,
                Ada = parselGrubu.Key.Ada,
                Parsel = parselGrubu.Key.Parsel,
                Birimler = birimler
            });
        }

        return sonuc.OrderBy(x => x.Il).ThenBy(x => x.Ada).ThenBy(x => x.Parsel).ToList();
    }
}
