using Ensek.Core.Configuration;
using Ensek.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Ensek.Test.Helpers;

public abstract class TestBase
{
    protected IConfiguration Configuration { get; }
    protected MeterReadingConfig MeterReadingConfig { get; }
    protected IOptions<MeterReadingConfig> MeterReadingOptions { get; }

    public TestBase()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["MeterReadingConfig:MinMeterReadingValue"] = "0",
                ["MeterReadingConfig:MaxMeterReadingValue"] = "99999"
            })
            .Build();

        MeterReadingConfig = new MeterReadingConfig();
        Configuration.GetSection("MeterReadingConfig").Bind(MeterReadingConfig);

        MeterReadingOptions = Options.Create(MeterReadingConfig);
    }

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