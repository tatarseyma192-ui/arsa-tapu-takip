using ArsaTapu.DataAccess.Context;
using ArsaTapu.DataAccess.Repositories;

namespace ArsaTapu.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ArsaTapuDbContext _context;

    public UnitOfWork(
        ArsaTapuDbContext context,
        IKisiRepository kisiler,
        ITasinmazRepository tasinmazlar,
        IParselKmlRepository parselKmlleri,
        IYuklemeKaydiRepository yuklemeKayitlari)
    {
        _context = context;
        Kisiler = kisiler;
        Tasinmazlar = tasinmazlar;
        ParselKmlleri = parselKmlleri;
        YuklemeKayitlari = yuklemeKayitlari;
    }

    public IKisiRepository Kisiler { get; }
    public ITasinmazRepository Tasinmazlar { get; }
    public IParselKmlRepository ParselKmlleri { get; }
    public IYuklemeKaydiRepository YuklemeKayitlari { get; }

    public Task<int> KaydetAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
