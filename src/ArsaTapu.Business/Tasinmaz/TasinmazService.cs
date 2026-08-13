using System.Linq.Expressions;
using ArsaTapu.Business.Common;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Enums;
using ArsaTapu.Domain.Exceptions;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Tasinmaz;
using Microsoft.EntityFrameworkCore;
using DomainTasinmaz = ArsaTapu.Domain.Entities.Tasinmaz;

namespace ArsaTapu.Business.Tasinmaz;

public class TasinmazService : ITasinmazService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IYetkiKapsamService _yetkiKapsam;
    private readonly IMulkiyetTekillestirmeService _tekillestirme;

    public TasinmazService(
        IUnitOfWork unitOfWork,
        IYetkiKapsamService yetkiKapsam,
        IMulkiyetTekillestirmeService tekillestirme)
    {
        _unitOfWork = unitOfWork;
        _yetkiKapsam = yetkiKapsam;
        _tekillestirme = tekillestirme;
    }

    private static readonly Expression<Func<DomainTasinmaz, TasinmazDto>> Projeksiyon = t => new TasinmazDto
    {
        Id = t.Id,
        KisiId = t.KisiId,
        KisiAdSoyad = t.Kisi != null ? t.Kisi.AdSoyad : null,
        TasinmazNo = t.TasinmazNo,
        Nitelik = t.Nitelik,
        Il = t.Il,
        Ilce = t.Ilce,
        Mahalle = t.Mahalle,
        Ada = t.Ada,
        Parsel = t.Parsel,
        BagimsizBolumNo = t.BagimsizBolumNo,
        ZeminHisseId = t.ZeminHisseId,
        Yuzolcum = t.Yuzolcum,
        Durum = t.Durum.ToString(),
        CreatedAt = t.CreatedAt
    };

    public async Task<PagedResult<TasinmazDto>> ListeleAsync(TasinmazFiltreDto filtre, CancellationToken ct = default)
    {
        var patronKisiId = await _yetkiKapsam.PatronKisiIdGetirAsync(ct);
        if (patronKisiId.HasValue)
        {
            // Requirements madde 1: Patron yalnızca kendi verisini görebilir — filtre burada zorlanır,
            // controller'dan gelen KisiId parametresi Patron için göz ardı edilir.
            filtre.KisiId = patronKisiId.Value;
        }

        var sorgu = _unitOfWork.Tasinmazlar.Sorgu(takipEtme: false);

        if (filtre.KisiId.HasValue) sorgu = sorgu.Where(x => x.KisiId == filtre.KisiId.Value);
        if (!string.IsNullOrWhiteSpace(filtre.Il)) sorgu = sorgu.Where(x => x.Il == filtre.Il);
        if (!string.IsNullOrWhiteSpace(filtre.Ilce)) sorgu = sorgu.Where(x => x.Ilce == filtre.Ilce);
        if (!string.IsNullOrWhiteSpace(filtre.Mahalle)) sorgu = sorgu.Where(x => x.Mahalle == filtre.Mahalle);
        if (filtre.Ada.HasValue) sorgu = sorgu.Where(x => x.Ada == filtre.Ada.Value);
        if (filtre.Parsel.HasValue) sorgu = sorgu.Where(x => x.Parsel == filtre.Parsel.Value);

        if (!string.IsNullOrWhiteSpace(filtre.Durum) && Enum.TryParse<TasinmazDurum>(filtre.Durum, true, out var durum))
        {
            sorgu = sorgu.Where(x => x.Durum == durum);
        }

        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            // TasinmazNo artık nullable olabildiği (bkz. Tasinmaz.cs) için tek başına arama
            // yetersiz kalırdı — Nitelik/Mahalle/ZeminHisseId de aramaya dahil edildi.
            var arama = filtre.Arama.Trim();
            sorgu = sorgu.Where(x =>
                (x.TasinmazNo != null && EF.Functions.ILike(x.TasinmazNo, $"%{arama}%")) ||
                EF.Functions.ILike(x.Nitelik, $"%{arama}%") ||
                EF.Functions.ILike(x.Mahalle, $"%{arama}%") ||
                EF.Functions.ILike(x.ZeminHisseId, $"%{arama}%"));
        }

        var toplam = await sorgu.CountAsync(ct);

        var kayitlar = await sorgu
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filtre.Sayfa - 1) * filtre.SayfaBoyutu)
            .Take(filtre.SayfaBoyutu)
            .Select(Projeksiyon)
            .ToListAsync(ct);

        return new PagedResult<TasinmazDto>
        {
            Kayitlar = kayitlar,
            ToplamKayit = toplam,
            Sayfa = filtre.Sayfa,
            SayfaBoyutu = filtre.SayfaBoyutu
        };
    }

    public async Task<TasinmazDto> GetirAsync(int id, CancellationToken ct = default)
    {
        var tasinmaz = await _unitOfWork.Tasinmazlar.Sorgu(takipEtme: false)
            .Where(x => x.Id == id)
            .Select(Projeksiyon)
            .FirstOrDefaultAsync(ct);

        if (tasinmaz is null) throw new NotFoundException("Taşınmaz", id);

        await _yetkiKapsam.KisiErisimKontrolEtAsync(tasinmaz.KisiId, ct);

        // Kullanıcı isteği: taşınmaz detayına bakarken, başkasıyla ortak/komşu olup olmadığı
        // görülsün. Yalnızca BURADA (tekil kayıt) hesaplanır — listelemede performans için hiç
        // çalıştırılmaz. Aynı Il/Ilce/Mahalle/Ada/Parsel'deki, BAŞKA kişilere ait, silinmemiş
        // kayıtlar tek sorguda çekilip bellekte ayrıştırılır (ortaklık vs. komşuluk).
        var ayniParseldekiDigerKayitlar = await _unitOfWork.Tasinmazlar.Sorgu(takipEtme: false)
            .Where(t => t.KisiId != tasinmaz.KisiId &&
                        t.Il == tasinmaz.Il && t.Ilce == tasinmaz.Ilce && t.Mahalle == tasinmaz.Mahalle &&
                        t.Ada == tasinmaz.Ada && t.Parsel == tasinmaz.Parsel)
            .Select(t => new { t.BagimsizBolumNo, t.ZeminHisseId, KisiAdi = t.Kisi != null ? t.Kisi.AdSoyad : null })
            .ToListAsync(ct);

        tasinmaz.OrtakKisiler = ayniParseldekiDigerKayitlar
            .Where(t => t.BagimsizBolumNo == tasinmaz.BagimsizBolumNo && t.ZeminHisseId == tasinmaz.ZeminHisseId)
            .Select(t => t.KisiAdi ?? "(isimsiz kişi)")
            .Distinct()
            .ToList();

        tasinmaz.KomsuKisiler = ayniParseldekiDigerKayitlar
            .Where(t => !(t.BagimsizBolumNo == tasinmaz.BagimsizBolumNo && t.ZeminHisseId == tasinmaz.ZeminHisseId))
            .Select(t => t.KisiAdi ?? "(isimsiz kişi)")
            .Distinct()
            .ToList();

        return tasinmaz;
    }

    public async Task<TasinmazDto> OlusturAsync(TasinmazCreateDto istek, CancellationToken ct = default)
    {
        var zatenVarMi = await _tekillestirme.KayitliMiAsync(
            istek.KisiId, istek.BagimsizBolumNo, istek.ZeminHisseId, ct);

        if (zatenVarMi)
        {
            throw new BusinessRuleException(
                "Bu taşınmaz (Bağımsız Bölüm No + Zemin Hisse ID) bu kişi için zaten kayıtlı.");
        }

        var tasinmaz = new DomainTasinmaz
        {
            KisiId = istek.KisiId,
            TasinmazNo = istek.TasinmazNo?.Trim(),
            Nitelik = istek.Nitelik.Trim(),
            Il = istek.Il.Trim(),
            Ilce = istek.Ilce.Trim(),
            Mahalle = istek.Mahalle.Trim(),
            Ada = istek.Ada,
            Parsel = istek.Parsel,
            BagimsizBolumNo = istek.BagimsizBolumNo,
            ZeminHisseId = istek.ZeminHisseId.Trim(),
            Yuzolcum = istek.Yuzolcum,
            Durum = TasinmazDurum.Aktif
        };

        await _unitOfWork.Tasinmazlar.EkleAsync(tasinmaz, ct);
        await _unitOfWork.KaydetAsync(ct);

        return await GetirAsync(tasinmaz.Id, ct);
    }

    public async Task<TasinmazDto> GuncelleAsync(int id, TasinmazUpdateDto istek, CancellationToken ct = default)
    {
        var tasinmaz = await _unitOfWork.Tasinmazlar.GetirAsync(id, ct)
            ?? throw new NotFoundException("Taşınmaz", id);

        await _yetkiKapsam.KisiErisimKontrolEtAsync(tasinmaz.KisiId, ct);

        if (!Enum.TryParse<TasinmazDurum>(istek.Durum, true, out var durum))
        {
            throw new BusinessRuleException($"Geçersiz durum değeri: {istek.Durum}. 'Aktif' veya 'Satildi' olmalı.");
        }

        tasinmaz.Nitelik = istek.Nitelik.Trim();
        tasinmaz.Il = istek.Il.Trim();
        tasinmaz.Ilce = istek.Ilce.Trim();
        tasinmaz.Mahalle = istek.Mahalle.Trim();
        tasinmaz.Ada = istek.Ada;
        tasinmaz.Parsel = istek.Parsel;
        tasinmaz.BagimsizBolumNo = istek.BagimsizBolumNo;
        tasinmaz.ZeminHisseId = istek.ZeminHisseId.Trim();
        tasinmaz.Yuzolcum = istek.Yuzolcum;
        tasinmaz.Durum = durum;

        _unitOfWork.Tasinmazlar.Guncelle(tasinmaz);
        await _unitOfWork.KaydetAsync(ct);

        return await GetirAsync(id, ct);
    }

    public async Task SilAsync(int id, CancellationToken ct = default)
    {
        var tasinmaz = await _unitOfWork.Tasinmazlar.GetirAsync(id, ct)
            ?? throw new NotFoundException("Taşınmaz", id);

        await _yetkiKapsam.TasinmazSilYetkisiKontrolEtAsync(tasinmaz, ct);

        _unitOfWork.Tasinmazlar.Sil(tasinmaz);
        await _unitOfWork.KaydetAsync(ct);
    }
}
