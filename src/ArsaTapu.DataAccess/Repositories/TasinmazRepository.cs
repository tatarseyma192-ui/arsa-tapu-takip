using ArsaTapu.DataAccess.Context;
using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.DataAccess.Repositories;

public class TasinmazRepository : Repository<Tasinmaz>, ITasinmazRepository
{
    public TasinmazRepository(ArsaTapuDbContext context) : base(context) { }

    public async Task<List<(int? BagimsizBolumNo, string ZeminHisseId)>> MevcutAnahtarlariGetirAsync(
        int kisiId, CancellationToken ct = default)
    {
        // Önce EF Core'un rahatça SQL'e çevirebildiği anonim tip projeksiyonu ile veritabanından çekilir,
        // sonra bellekte adlandırılmış tuple'a dönüştürülür (tuple literal'ların IQueryable projeksiyonunda
        // isim uyuşmazlığı riskini tamamen ortadan kaldırmak için).
        var kayitlar = await DbSet.AsNoTracking()
            .Where(x => x.KisiId == kisiId)
            .Select(x => new { x.BagimsizBolumNo, x.ZeminHisseId })
            .ToListAsync(ct);

        return kayitlar
            .Select(x => (x.BagimsizBolumNo, x.ZeminHisseId))
            .ToList();
    }
}
