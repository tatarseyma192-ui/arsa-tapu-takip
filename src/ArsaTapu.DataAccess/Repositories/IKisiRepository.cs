using ArsaTapu.Domain.Entities;

namespace ArsaTapu.DataAccess.Repositories;

public interface IKisiRepository : IRepository<Kisi>
{
    Task<Kisi?> KullaniciIdIleGetirAsync(string kullaniciId, CancellationToken ct = default);
}
