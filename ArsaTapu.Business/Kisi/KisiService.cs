using System.Linq.Expressions;
using ArsaTapu.Business.Common;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Kisi;
using Microsoft.EntityFrameworkCore;
using DomainKisi = ArsaTapu.Domain.Entities.Kisi;

namespace ArsaTapu.Business.Kisi;

public class KisiService : IKisiService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYetkiKapsamService _yetkiKapsam;

    public KisiService(IUnitOfWork unitOfWork, IYetkiKapsamService yetkiKapsam)
    {
        _unitOfWork = unitOfWork;
        _yetkiKapsam = yetkiKapsam;
    }

    // EF Core'un SQL'e çevirebilmesi için Expression olarak tanımlanır (navigation Count() dahil).
    private static readonly Expression<Func<DomainKisi, KisiDto>> Projeksiyon = kisi => new KisiDto
    {
        Id = kisi.Id,
        AdSoyad = kisi.AdSoyad,
        KullaniciId = kisi.KullaniciId,
        AktifTasinmazSayisi = kisi.Tasinmazlar.Count(t => t.Durum == TasinmazDurum.Aktif),
        SatilanTasinmazSayisi = kisi.Tasinmazlar.Count(t => t.Durum == TasinmazDurum.Satildi),
        YuklemeSayisi = kisi.YuklemeKayitlari.Count()
    };

    public async Task<PagedResult<KisiDto>> ListeleAsync(PagedRequest istek, CancellationToken ct = default)
    {
        var sorgu = _unitOfWork.Kisiler.Sorgu(takipEtme: false);

        if (!string.IsNullOrWhiteSpace(istek.Arama))
        {
            var arama = istek.Arama.Trim();
            sorgu = sorgu.Where(x => EF.Functions.ILike(x.AdSoyad, $"%{arama}%"));
        }

        var toplam = await sorgu.CountAsync(ct);

        var kayitlar = await sorgu
            .OrderBy(x => x.AdSoyad)
            .Skip((istek.Sayfa - 1) * istek.SayfaBoyutu)
            .Take(istek.SayfaBoyutu)
            .Select(Projeksiyon)
            .ToListAsync(ct);

        return new PagedResult<KisiDto>
        {
            Kayitlar = kayitlar,
            ToplamKayit = toplam,
            Sayfa = istek.Sayfa,
            SayfaBoyutu = istek.SayfaBoyutu
        };
    }

    public async Task<KisiDto> GetirAsync(int id, CancellationToken ct = default)
    {
        await _yetkiKapsam.KisiErisimKontrolEtAsync(id, ct);

        var kisi = await _unitOfWork.Kisiler.Sorgu(takipEtme: false)
            .Where(x => x.Id == id)
            .Select(Projeksiyon)
            .FirstOrDefaultAsync(ct);

        return kisi ?? throw new NotFoundException("Kişi", id);
    }

    public async Task<KisiDto?> KendiProfiliniGetirAsync(CancellationToken ct = default)
    {
        var patronKisiId = await _yetkiKapsam.PatronKisiIdGetirAsync(ct);
        if (!patronKisiId.HasValue) return null;

        return await GetirAsync(patronKisiId.Value, ct);
    }

    public async Task<KisiDto> OlusturAsync(KisiCreateDto istek, CancellationToken ct = default)
    {
        var kisi = new DomainKisi
        {
            AdSoyad = istek.AdSoyad.Trim(),
            KullaniciId = istek.KullaniciId
        };

        await _unitOfWork.Kisiler.EkleAsync(kisi, ct);
        await _unitOfWork.KaydetAsync(ct);

        return await GetirAsync(kisi.Id, ct);
    }

    public async Task<KisiDto> GuncelleAsync(int id, KisiUpdateDto istek, CancellationToken ct = default)
    {
        var kisi = await _unitOfWork.Kisiler.GetirAsync(id, ct)
            ?? throw new NotFoundException("Kişi", id);

        kisi.AdSoyad = istek.AdSoyad.Trim();
        kisi.KullaniciId = istek.KullaniciId;

        _unitOfWork.Kisiler.Guncelle(kisi);
        await _unitOfWork.KaydetAsync(ct);

        return await GetirAsync(id, ct);
    }

    public async Task SilAsync(int id, CancellationToken ct = default)
    {
        var kisi = await _unitOfWork.Kisiler.GetirAsync(id, ct)
            ?? throw new NotFoundException("Kişi", id);

        _unitOfWork.Kisiler.Sil(kisi);
        await _unitOfWork.KaydetAsync(ct);
    }
}
