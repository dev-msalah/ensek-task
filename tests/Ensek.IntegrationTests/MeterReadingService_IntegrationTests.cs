using Ensek.Core.Configuration;
using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;
using Ensek.Core.Models;
using Ensek.Infrastructure;
using Ensek.Services;
using Ensek.Test.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ensek.IntegrationTests;

public class MeterReadingService_IntegrationTests : TestBase
{
    private readonly AppDbContext _context;
    private readonly IMeterReadingService _service;
    private readonly IMeterReadingValidator _validator;

    public MeterReadingService_IntegrationTests()
    {
        _context = CreateContext();

        var config = new MeterReadingConfig();
        Configuration.GetSection("MeterReadingConfig").Bind(config);
        _validator = new MeterReadingValidator(config);
        _service = new MeterReadingService(_context, _validator, NullLogger<MeterReadingService>.Instance);
    }


    [Fact]
    public async Task Invalid_DTO_Returns_Failure()
    {
        var dto = new MeterReadingDto
        {
            AccountId = -1,
            MeterReadingDateTime = default,
            MeterReadValue = -1
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason == "AccountId is invalid.");
    }
    [Fact]
    public async Task Account_Does_Not_Exist_Returns_Failure()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 999,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason == "Account does not exist");
    }

    [Fact]
    public async Task Duplicate_Reading_Is_Rejected()
    {
        var existing = new MeterReading
        {
            AccountId = 123,
            MeterReadValue = 12345,
            MeterReadingDateTime = new DateTime(2024, 04, 05, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.MeterReadings.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new MeterReadingDto
        {
            AccountId = existing.AccountId,
            MeterReadValue = existing.MeterReadValue,
            MeterReadingDateTime = new DateTime(2024, 04, 05, 0, 0, 0, DateTimeKind.Utc)
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason == "Duplicate reading");
    }


    [Fact]
    public async Task Older_Reading_Than_Existing_Is_Rejected()
    {
        var existing = new MeterReading
        {
            AccountId = 123,
            MeterReadValue = 54321,
            MeterReadingDateTime = new DateTime(2024, 04, 05, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.MeterReadings.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new MeterReadingDto
        {
            AccountId = existing.AccountId,
            MeterReadValue = 12345,
            MeterReadingDateTime = new DateTime(2024, 04, 04, 0, 0, 0, DateTimeKind.Utc) 
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason == "Reading is older or equal to the latest one");
    }

    [Fact]
    public async Task Valid_Reading_Is_Saved_Successfully()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 06),
            MeterReadValue = 54321
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(1, result.Successful);
        Assert.Equal(0, result.Failed);

        var saved = await _context.MeterReadings.FirstOrDefaultAsync(r => r.AccountId == dto.AccountId);
        Assert.NotNull(saved);
        Assert.Equal(dto.MeterReadValue, saved.MeterReadValue);
        Assert.Equal(dto.MeterReadingDateTime.Date, saved.MeterReadingDateTime.Date);
    }

    [Fact]
    public async Task Multiple_Readings_Processed_Correctly()
    {
        var dtos = new[]
        {
            new MeterReadingDto { AccountId = 123, MeterReadingDateTime = new DateTime(2024, 04, 06), MeterReadValue = 54321 },
            new MeterReadingDto { AccountId = 123, MeterReadingDateTime = new DateTime(2024, 04, 07), MeterReadValue = 54322 },
            new MeterReadingDto { AccountId = 999, MeterReadingDateTime = new DateTime(2024, 04, 07), MeterReadValue = 12345 }, 
            new MeterReadingDto { AccountId = 123, MeterReadingDateTime = new DateTime(2024, 04, 06), MeterReadValue = 54321 } 
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(dtos);

        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(2, result.Successful); 
        Assert.Equal(2, result.Failed);     

        var savedCount = await _context.MeterReadings.CountAsync();
        Assert.Equal(2, savedCount);
    }
}
