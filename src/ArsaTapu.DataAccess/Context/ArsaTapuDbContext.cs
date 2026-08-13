using ArsaTapu.DataAccess.Entities;
using ArsaTapu.DataAccess.Identity;
using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.DataAccess.Context;

public class ArsaTapuDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private readonly ICurrentUserService? _currentUserService;

    public ArsaTapuDbContext(
        DbContextOptions<ArsaTapuDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Kisi> Kisiler => Set<Kisi>();
    public DbSet<YuklemeKaydi> YuklemeKayitlari => Set<YuklemeKaydi>();
    public DbSet<Tasinmaz> Tasinmazlar => Set<Tasinmaz>();
    public DbSet<ParselKml> ParselKmlleri => Set<ParselKml>();
    public DbSet<RefreshToken> RefreshTokenlar => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ArsaTapuDbContext).Assembly);

        // Soft delete: Handbook madde 5. Silinen kayıtlar sorgulara otomatik dahil edilmez.
        builder.Entity<Kisi>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<YuklemeKaydi>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Tasinmaz>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ParselKml>().HasQueryFilter(x => !x.IsDeleted);
    }

    public override int SaveChanges()
    {
        UygulaAuditVeSoftDelete();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UygulaAuditVeSoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ekleme/güncelleme audit alanlarını otomatik doldurur ve fiziksel silmeyi
    /// soft delete'e çevirir (Entity Framework değişiklik izleyicisi üzerinden).
    /// </summary>
    private void UygulaAuditVeSoftDelete()
    {
        var kullanici = _currentUserService?.UserId ?? "sistem";
        var simdi = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = simdi;
                    entry.Entity.CreatedBy = kullanici;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = simdi;
                    entry.Entity.UpdatedBy = kullanici;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = simdi;
                    entry.Entity.UpdatedBy = kullanici;
                    break;
            }
        }
    }
}
