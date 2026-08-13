using ArsaTapu.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace ArsaTapu.Tests.TestYardimcilari;

public static class TestDbContextFactory
{
    public static ArsaTapuDbContext Olustur()
    {
        var options = new DbContextOptionsBuilder<ArsaTapuDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ArsaTapuDbContext(options, currentUserService: null);
    }
}
