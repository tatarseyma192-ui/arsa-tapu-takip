using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.Tekillestirme;

public class KmlTekillestirmeService : IKmlTekillestirmeService
{
    private readonly IUnitOfWork _unitOfWork;

    public KmlTekillestirmeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> CekilmisMiAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default)
    {
        var mevcutAnahtarlar = await _unitOfWork.ParselKmlleri.BasariliAnahtarlariGetirAsync(ct);
        return mevcutAnahtarlar.Any(a =>
            a.Il == il && a.Ilce == ilce && a.Mahalle == mahalle && a.Ada == ada && a.Parsel == parsel);
    }

    public async Task<KmlTekillestirmeSonucuDto> SiniflandirAsync(
        IReadOnlyList<ParselAdayDto> adaylar, CancellationToken ct = default)
    {
        var mevcutAnahtarlar = (await _unitOfWork.ParselKmlleri.BasariliAnahtarlariGetirAsync(ct))
            .Select(a => (a.Il, a.Ilce, a.Mahalle, a.Ada, a.Parsel))
            .ToHashSet();

        var sonuc = new KmlTekillestirmeSonucuDto();
        var buPartideGorulenler = new HashSet<(string, string, string, int, int)>();

        foreach (var aday in adaylar)
        {
            var anahtar = (aday.Il, aday.Ilce, aday.Mahalle, aday.Ada, aday.Parsel);

            if (mevcutAnahtarlar.Contains(anahtar))
            {
                sonuc.ZatenCekilmisOlanlar.Add(aday);
                continue;
            }

            // Aynı ada/parsel'e bağlı birden fazla taşınmaz olsa da yalnızca 1 kez sorgu listesine eklenir.
            if (buPartideGorulenler.Add(anahtar))
                sonuc.SorgulanmasiGerekenler.Add(aday);
        }

        return sonuc;
    }
}
