using ArsaTapu.DataAccess.Context;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.DataAccess.Repositories;

public class ParselKmlRepository : Repository<ParselKml>, IParselKmlRepository
{
    public ParselKmlRepository(ArsaTapuDbContext context) : base(context) { }

    public async Task<List<(string Il, string Ilce, string Mahalle, int Ada, int Parsel)>> BasariliAnahtarlariGetirAsync(
        CancellationToken ct = default)
    {
        var kayitlar = await DbSet.AsNoTracking()
            .Where(x => x.Durum == KmlDurum.Basarili)
            .Select(x => new { x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel })
            .ToListAsync(ct);

        return kayitlar
            .Select(x => (x.Il, x.Ilce, x.Mahalle, x.Ada, x.Parsel))
            .ToList();
    }

    public async Task<ParselKml?> AnahtarIleBulAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(x =>
            x.Il == il && x.Ilce == ilce && x.Mahalle == mahalle && x.Ada == ada && x.Parsel == parsel, ct);
}
