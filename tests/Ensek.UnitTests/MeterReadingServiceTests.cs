using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;
using Ensek.Core.Models;
using Ensek.Infrastructure;
using Ensek.Services;
using Ensek.Test.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ensek.UnitTests.Services;

public class MeterReadingServiceTests: TestBase
{
    private readonly AppDbContext _context;
    private readonly IMeterReadingService _service;
    private readonly IMeterReadingValidator _validator;

    public MeterReadingServiceTests()
    {
        _context = CreateContext();
        _validator = new MeterReadingValidator(MeterReadingOptions);
        _service = new MeterReadingService(_context, _validator, NullLogger<MeterReadingService>.Instance);
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
        Assert.Contains(result.Failures, f => f.Reading.AccountId == dto.AccountId);
    }

    [Fact]
    public async Task Duplicate_Reading_Is_Rejected()
    {
        var testDate = new DateTime(2024, 12, 20, 0, 0, 0);
        var existing = new MeterReading
        {
            AccountId = 123,
            MeterReadValue = 12345,
            MeterReadingDateTime = testDate
        };

        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadValue = 12345,
            MeterReadingDateTime = testDate
        };

        _context.MeterReadings.Add(existing);
        await _context.SaveChangesAsync();

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
            MeterReadingDateTime = new DateTime(2024, 04, 05)
        };

        var dto = new MeterReadingDto
        {
            AccountId = existing.AccountId,
            MeterReadValue = 12345,
            MeterReadingDateTime = new DateTime(2024, 04, 04) 
        };

        _context.MeterReadings.Add(existing);
        await _context.SaveChangesAsync();

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
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var memoryStream = TestCsvHelper.CreateCsvStream(new[] { dto });
        var result = await _service.ProcessReadingsAsync(memoryStream);

        Assert.Equal(1, result.Successful);
        Assert.Equal(0, result.Failed);
        Assert.True(await _context.MeterReadings.AnyAsync(r => r.AccountId == dto.AccountId));
    }

    [Fact]
    public async Task Empty_Csv_Returns_No_Readings()
    {
        using var emptyStream = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _service.ProcessReadingsAsync(emptyStream));
        Assert.Contains("CSV file format is invalid", exception.Message);
    }

    [Fact]
    public async Task Malformed_Csv_Throws_InvalidDataException()
    {
        var malformedContent = "bad_header1,bad_header2\ninvalid,data\n";
        var malformedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(malformedContent));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _service.ProcessReadingsAsync(malformedStream));
        Assert.Contains("CSV file format is invalid", exception.Message);
    }

    [Fact]
    public async Task Invalid_Record_Is_Rejected()
    {
        var invalidContent = "AccountId,MeterReadingDateTime,MeterReadValue\n,invalid_date,NaN\n";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidContent));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _service.ProcessReadingsAsync(stream));
        Assert.Contains("CSV file format is invalid", exception.Message);
    }

    [Fact]
    public async Task Invalid_Reading_Fails_Validation()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = DateTime.MinValue, 
            MeterReadValue = -1                       
        };

        var stream = TestCsvHelper.CreateCsvStream(new[] { dto });

        var result = await _service.ProcessReadingsAsync(stream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason.Contains("MeterReadValue must be between 0 and 99999."));
    }

    [Fact]
    public async Task Reading_With_Equal_Date_To_Latest_Is_Rejected()
    {
        var date = new DateTime(2024, 04, 05);
        _context.MeterReadings.Add(new MeterReading
        {
            AccountId = 123,
            MeterReadingDateTime = date,
            MeterReadValue = 10000
        });
        await _context.SaveChangesAsync();

        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = date, 
            MeterReadValue = 9999
        };

        var stream = TestCsvHelper.CreateCsvStream(new[] { dto });
        var result = await _service.ProcessReadingsAsync(stream);

        Assert.Equal(0, result.Successful);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Failures, f => f.Reason == "Reading is older or equal to the latest one");
    }

    [Fact]
    public async Task Multiple_Readings_Mixed_Success_And_Failure()
    {
        _context.MeterReadings.Add(new MeterReading
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 54321
        });
        await _context.SaveChangesAsync();

        var validDto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 06),
            MeterReadValue = 67890
        };

        var duplicateDto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 54321
        };

        var invalidDto = new MeterReadingDto
        {
            AccountId = 999, 
            MeterReadingDateTime = new DateTime(2024, 04, 07),
            MeterReadValue = 11111
        };

        var stream = TestCsvHelper.CreateCsvStream(new[] { validDto, duplicateDto, invalidDto });
        var result = await _service.ProcessReadingsAsync(stream);

        Assert.Equal(1, result.Successful);
        Assert.Equal(2, result.Failed);
    }
}