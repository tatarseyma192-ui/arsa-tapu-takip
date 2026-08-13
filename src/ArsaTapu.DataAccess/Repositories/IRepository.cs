namespace ArsaTapu.DataAccess.Repositories;

/// <summary>Generic repository — ortak CRUD işlemleri (Handbook madde 7: ortak işlemler ortak yapılarda toplanmalı).</summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Sorgu(bool takipEtme = true);
    Task<T?> GetirAsync(int id, CancellationToken ct = default);
    Task EkleAsync(T entity, CancellationToken ct = default);
    void Guncelle(T entity);
    void Sil(T entity);
}
