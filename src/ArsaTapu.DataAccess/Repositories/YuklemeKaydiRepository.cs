using ArsaTapu.DataAccess.Context;
using ArsaTapu.Domain.Entities;

namespace ArsaTapu.DataAccess.Repositories;

public class YuklemeKaydiRepository : Repository<YuklemeKaydi>, IYuklemeKaydiRepository
{
    public YuklemeKaydiRepository(ArsaTapuDbContext context) : base(context) { }
}
