using Ensek.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ensek.Test.Helpers;

public abstract class TestBase
{
    protected AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "SharedTestDb_" + Guid.NewGuid())
            .Options;

        var context = new AppDbContext(options);
        TestDatabaseSeeder.SeedTestDataAsync(context).Wait();
        return context;
    }
}