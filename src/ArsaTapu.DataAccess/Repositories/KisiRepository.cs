using ArsaTapu.DataAccess.Context;
using ArsaTapu.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.DataAccess.Repositories;

public class KisiRepository : Repository<Kisi>, IKisiRepository
{
    public KisiRepository(ArsaTapuDbContext context) : base(context) { }

    public async Task<Kisi?> KullaniciIdIleGetirAsync(string kullaniciId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId, ct);
}
