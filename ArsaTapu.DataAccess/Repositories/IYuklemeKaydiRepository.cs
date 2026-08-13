namespace ArsaTapu.DataAccess.Repositories;

/// <summary>
/// Şimdilik generic CRUD yeterli. PDF/Excel parse motoru geldiğinde (ayrı adım)
/// bu arayüze özel sorgu metodları eklenecektir.
/// </summary>
public interface IYuklemeKaydiRepository : IRepository<Domain.Entities.YuklemeKaydi>
{
}
