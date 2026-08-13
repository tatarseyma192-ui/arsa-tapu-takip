using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.Tekillestirme;

public class MulkiyetTekillestirmeService : IMulkiyetTekillestirmeService
{
    private readonly IUnitOfWork _unitOfWork;

    public MulkiyetTekillestirmeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> KayitliMiAsync(
        int kisiId, int? bagimsizBolumNo, string zeminHisseId, CancellationToken ct = default)
    {
        var mevcutAnahtarlar = await _unitOfWork.Tasinmazlar.MevcutAnahtarlariGetirAsync(kisiId, ct);
        return mevcutAnahtarlar.Any(a =>
            a.BagimsizBolumNo == bagimsizBolumNo &&
            a.ZeminHisseId == zeminHisseId);
    }

    public async Task<MulkiyetTekillestirmeSonucuDto> SiniflandirAsync(
        int kisiId, IReadOnlyList<MulkiyetAdayDto> adaylar, CancellationToken ct = default)
    {
        var mevcutAnahtarlar = (await _unitOfWork.Tasinmazlar.MevcutAnahtarlariGetirAsync(kisiId, ct))
            .Select(a => (a.BagimsizBolumNo, a.ZeminHisseId))
            .ToHashSet();

        var sonuc = new MulkiyetTekillestirmeSonucuDto();

        foreach (var aday in adaylar)
        {
            var anahtar = (aday.BagimsizBolumNo, aday.ZeminHisseId);

            if (mevcutAnahtarlar.Contains(anahtar))
                sonuc.ZatenKayitliOlanlar.Add(aday);
            else
                sonuc.YeniAlimlar.Add(aday);
        }

        return sonuc;
    }
}
