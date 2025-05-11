using CsvHelper;
using System.Globalization;
using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;
using Ensek.Infrastructure;
using Ensek.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ensek.Core.Results;

namespace Ensek.Services;

public class MeterReadingService(AppDbContext context, IMeterReadingValidator validator, ILogger<MeterReadingService> logger) : IMeterReadingService
{
    private readonly AppDbContext _context = context;
    private readonly IMeterReadingValidator _validator = validator;
    private readonly ILogger<MeterReadingService> _logger = logger;

    public async Task<MeterReadingProcessingResult> ProcessReadingsAsync(Stream csvStream, CancellationToken ct = default)
    {
        var result = new MeterReadingProcessingResult();
        var accountIds = await _context.Accounts.Select(a => a.AccountId).ToListAsync(ct);

        var newReadings = new List<MeterReading>();
        var seenKeys = new HashSet<string>();
        var accountReadingsCache = new Dictionary<int, List<MeterReading>>();
        var latestReadingsInMemory = new Dictionary<int, MeterReading>();
        
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("en-GB"));

        try
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var record = csv.GetRecord<MeterReadingDto>();

                if (record == null)
                {
                    AddFailure(result, new MeterReadingDto { }, "Empty or invalid record");
                    continue;
                }

                if (!_validator.IsValid(record, out string? errorMessage))
                {
                    AddFailure(result, record, errorMessage ?? "Validation failed");
                    continue;
                }

                if (!accountIds.Contains(record.AccountId))
                {
                    AddFailure(result, record, "Account does not exist");
                    continue;
                }

                var existingReadings = await GetOrLoadAccountReadings(record.AccountId, accountReadingsCache, ct);

                if (IsDuplicateReading(record, existingReadings, seenKeys))
                {
                    AddFailure(result, record, "Duplicate reading");
                    continue;
                }

                var latestReading = await GetLatestReading(record.AccountId, existingReadings, latestReadingsInMemory);

                if(latestReading != null && record.MeterReadingDateTime <= latestReading.MeterReadingDateTime)
                {
                    AddFailure(result, record, "Reading is older or equal to the latest one");
                    continue;
                }

                var entity = new MeterReading
                {
                    AccountId = record.AccountId,
                    MeterReadingDateTime = record.MeterReadingDateTime,
                    MeterReadValue = record.MeterReadValue
                };

                newReadings.Add(entity);

                UpdateLatestReadingInMemory(record, entity, latestReadingsInMemory);

                result.Successful++;
            }
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError(ex, "Failed to parse CSV file.");
            throw new InvalidDataException("CSV file format is invalid. Please check headers and data format.", ex);
        }

        LogProcessingFailures(result);

        if (newReadings.Count > 0)
        {
            await _context.MeterReadings.AddRangeAsync(newReadings, ct);
            await _context.SaveChangesAsync(ct);
        }

        return result;
    }

    private async Task<List<MeterReading>> GetOrLoadAccountReadings(int accountId, Dictionary<int, List<MeterReading>> cache, CancellationToken ct)
    {
        if (!cache.TryGetValue(accountId, out var existingReadings))
        {
            existingReadings = await _context.MeterReadings
                .Where(r => r.AccountId == accountId)
                .ToListAsync(ct);
            cache[accountId] = existingReadings;
        }
        return existingReadings;
    }

    private bool IsDuplicateReading(MeterReadingDto record, List<MeterReading> existingReadings, HashSet<string> seenKeys)
    {
        var isDuplicateDb = existingReadings.Any(r =>
            r.MeterReadValue == record.MeterReadValue &&
            r.MeterReadingDateTime.Ticks == record.MeterReadingDateTime.Ticks);

        var key = $"{record.AccountId}|{record.MeterReadValue}|{record.MeterReadingDateTime.Ticks}";
        var isDuplicateInMemory = !seenKeys.Add(key);

        return isDuplicateDb || isDuplicateInMemory;
    }

    private async Task<MeterReading?> GetLatestReading(int accountId, List<MeterReading> existingReadings, Dictionary<int, MeterReading> latestReadingsInMemory)
    {
        var latestReadingDb = existingReadings
            .OrderByDescending(r => r.MeterReadingDateTime)
            .FirstOrDefault();

        latestReadingsInMemory.TryGetValue(accountId, out var latestReadingInMemory);

        return (latestReadingDb, latestReadingInMemory) switch
        {
            (null, null) => null,
            (not null, null) => latestReadingDb,
            (null, not null) => latestReadingInMemory,
            _ => latestReadingDb!.MeterReadingDateTime > latestReadingInMemory!.MeterReadingDateTime
                ? latestReadingDb
                : latestReadingInMemory
        };
    }

    private void UpdateLatestReadingInMemory(MeterReadingDto record, MeterReading entity, Dictionary<int, MeterReading> latestReadingsInMemory)
    {
        if (!latestReadingsInMemory.TryGetValue(record.AccountId, out var currentLatest) ||
            record.MeterReadingDateTime > currentLatest.MeterReadingDateTime)
        {
            latestReadingsInMemory[record.AccountId] = entity;
        }
    }

    private void AddFailure(MeterReadingProcessingResult result, MeterReadingDto record, string reason)
    {
        result.Failed++;
        result.Failures.Add(new MeterReadingFailureDetail
        {
            Reading = record,
            Reason = reason
        });
    }

    private void LogProcessingFailures(MeterReadingProcessingResult result)
    {
        foreach (var failure in result.Failures)
        {
            _logger.LogWarning("Failed to process reading: {@Reading}, Reason: {Reason}",
                failure.Reading, failure.Reason);
        }
    }
 
    //public async Task<MeterReadingProcessingResult> ProcessReadingsAsync(Stream csvStream, CancellationToken ct = default)
    //{
    //    var result = new MeterReadingProcessingResult();

    //    using var reader = new StreamReader(csvStream);
    //    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    //    List<MeterReadingDto>? records;

    //    try
    //    {
    //        records = csv.GetRecords<MeterReadingDto>().ToList();
    //    }
    //    catch (CsvHelperException ex)
    //    {
    //        _logger.LogError(ex, "Failed to parse CSV file.");
    //        throw new InvalidDataException("CSV file format is invalid. Please check headers and data format.", ex);
    //    }

    //    foreach (var record in records)
    //    {
    //        if (!_validator.IsValid(record, out string? errorMessage))
    //        {
    //            result.Failed++;
    //            result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = errorMessage ?? "Validation failed" });
    //            continue;
    //        }

    //        if (!await _context.Accounts.AnyAsync(a => a.AccountId == record.AccountId, ct))
    //        {
    //            result.Failed++;
    //            result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = "Account does not exist" });
    //            continue;
    //        }

    //        if (await _context.MeterReadings.AnyAsync(r =>
    //                r.AccountId == record.AccountId &&
    //                r.MeterReadValue == record.MeterReadValue &&
    //                r.MeterReadingDateTime == record.MeterReadingDateTime, ct))
    //        {
    //            result.Failed++;
    //            result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = "Duplicate reading" });
    //            continue;
    //        }

    //        var latestReading = await _context.MeterReadings
    //            .Where(r => r.AccountId == record.AccountId)
    //            .OrderByDescending(r => r.MeterReadingDateTime)
    //            .FirstOrDefaultAsync(ct);

    //        if (latestReading != null && record.MeterReadingDateTime <= latestReading.MeterReadingDateTime)
    //        {
    //            result.Failed++;
    //            result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = "Reading is older or equal to the latest one" });
    //            continue;
    //        }

    //        await _context.MeterReadings.AddAsync(new MeterReading
    //        {
    //            AccountId = record.AccountId,
    //            MeterReadingDateTime = record.MeterReadingDateTime,
    //            MeterReadValue = record.MeterReadValue
    //        }, ct);

    //        result.Successful++;
    //    }

    //    foreach (var failure in result.Failures)
    //    {
    //        _logger.LogWarning("Failed to process reading: {@Reading}, Reason: {Reason}", failure.Reading, failure.Reason);
    //    }

    //    await _context.SaveChangesAsync();
    //    return result;
    //}
}