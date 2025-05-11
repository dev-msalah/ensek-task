using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;
using Ensek.Infrastructure;
using Ensek.Services;
using Ensek.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ensek.IntegrationTests;

public class LargeFilePerformanceTests : TestBase
{
    private readonly AppDbContext _context;
    private readonly IMeterReadingValidator _validator;
    private readonly IMeterReadingService _service;

    public LargeFilePerformanceTests()
    {
        _context = CreateContext();
        _validator = new MeterReadingValidator(MeterReadingOptions);
        _service = new MeterReadingService(_context, _validator, NullLogger<MeterReadingService>.Instance);
    }

    [Fact]
    public async Task Process_Readings_With_150000_Valid_Records()
    {
        var validAccountId = 123;

        var dtos = new List<MeterReadingDto>();
        for (int i = 0; i < 150000; i++)
        {
            dtos.Add(new MeterReadingDto
            {
                AccountId = validAccountId,
                MeterReadingDateTime = new DateTime(2024, 01, 1)
                    .AddDays(i)
                    .AddHours(8),
                MeterReadValue = i % 99999
            });
        }

        var stream = TestCsvHelper.CreateCsvStream(dtos);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.ProcessReadingsAsync(stream);
        stopwatch.Stop();

        Assert.Equal(150000, result.Successful);
        Assert.Equal(0, result.Failed);
        Assert.True(result.Failures.Count == 0);

        var totalSeconds = stopwatch.Elapsed.TotalSeconds;
        Assert.InRange(totalSeconds, 0, 3); 
    }
}