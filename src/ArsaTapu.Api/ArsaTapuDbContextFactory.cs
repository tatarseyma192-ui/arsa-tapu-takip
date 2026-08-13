using ArsaTapu.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArsaTapu.Api;

/// <summary>
/// EF Core CLI araçları (dotnet ef migrations add / database update) için tasarım-zamanı
/// DbContext üreticisi. Program.cs'in tam host kurulumunu (Jwt:Key/appsettings kontrolleri,
/// Identity, JWT Bearer ayarları vb.) ATLAYARAK doğrudan bir DbContext örneği kurar — CI/CD'de
/// yalnızca bir ortam değişkeniyle migration çalıştırmayı basitleştirir, appsettings/user-secrets
/// gerektirmez. Bu, EF Core'un IDesignTimeDbContextFactory için ÖNERDİĞİ standart yaklaşımdır.
/// </summary>
public class ArsaTapuDbContextFactory : IDesignTimeDbContextFactory<ArsaTapuDbContext>
{
    public ArsaTapuDbContext CreateDbContext(string[] args)
    {
        var baglantiDizesi = Environment.GetEnvironmentVariable("ConnectionStrings__VarsayilanBaglanti")
            ?? "Host=localhost;Port=5432;Database=arsatapu;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ArsaTapuDbContext>();
        optionsBuilder.UseNpgsql(baglantiDizesi);

        return new ArsaTapuDbContext(optionsBuilder.Options, currentUserService: null);
    }
}
