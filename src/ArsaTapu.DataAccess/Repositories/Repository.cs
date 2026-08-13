using ArsaTapu.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.DataAccess.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ArsaTapuDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ArsaTapuDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public IQueryable<T> Sorgu(bool takipEtme = true) =>
        takipEtme ? DbSet : DbSet.AsNoTracking();

    public async Task<T?> GetirAsync(int id, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { id }, ct);

    public async Task EkleAsync(T entity, CancellationToken ct = default) =>
        await DbSet.AddAsync(entity, ct);

    public void Guncelle(T entity) => DbSet.Update(entity);

    public void Sil(T entity) => DbSet.Remove(entity);
}
