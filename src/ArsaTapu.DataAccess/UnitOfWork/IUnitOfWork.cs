using ArsaTapu.DataAccess.Repositories;

namespace ArsaTapu.DataAccess.UnitOfWork;

public interface IUnitOfWork
{
    IKisiRepository Kisiler { get; }
    ITasinmazRepository Tasinmazlar { get; }
    IParselKmlRepository ParselKmlleri { get; }
    IYuklemeKaydiRepository YuklemeKayitlari { get; }

    Task<int> KaydetAsync(CancellationToken ct = default);
}
