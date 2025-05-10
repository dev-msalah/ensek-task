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
        var latestReadingsInMemory = new Dictionary<int, MeterReading>();
        var _accountReadingsCache = new Dictionary<int, List<MeterReading>>();

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
                    result.Failed++;
                    result.Failures.Add(new MeterReadingFailureDetail { Reading = record!, Reason = "Empty or invalid record" });
                    continue;
                }

                if (!_validator.IsValid(record, out string? errorMessage))
                {
                    result.Failed++;
                    result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = errorMessage ?? "Validation failed" });
                    continue;
                }

                if (!accountIds.Contains(record.AccountId))
                {
                    result.Failed++;
                    result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = "Account does not exist" });
                    continue;
                }

                if (!_accountReadingsCache.TryGetValue(record.AccountId, out var existingReadings))
                {
                    existingReadings = await _context.MeterReadings
                        .Where(r => r.AccountId == record.AccountId)
                        .ToListAsync(ct);

                    _accountReadingsCache[record.AccountId] = existingReadings;
                }

                var isDuplicateDb = existingReadings.Any(r =>
                     r.MeterReadValue == record.MeterReadValue &&
                     r.MeterReadingDateTime.Ticks == record.MeterReadingDateTime.Ticks);

                var key = $"{record.AccountId}|{record.MeterReadValue}|{record.MeterReadingDateTime.Ticks}";
                var isDuplicateInMemory = !seenKeys.Add(key);

                if (isDuplicateDb || isDuplicateInMemory)
                {
                    result.Failed++;
                    result.Failures.Add(new MeterReadingFailureDetail { Reading = record, Reason = "Duplicate reading" });
                    continue;
                }

                var latestReadingDb = existingReadings
                .OrderByDescending(r => r.MeterReadingDateTime)
                 .FirstOrDefault();
                
                latestReadingsInMemory.TryGetValue(record.AccountId, out var latestReadingInMemory);

                var latestReading = (latestReadingDb, latestReadingInMemory) switch
                {
                    (null, null) => null,
                    (not null, null) => latestReadingDb,
                    (null, not null) => latestReadingInMemory,
                    _ => latestReadingDb.MeterReadingDateTime > latestReadingInMemory.MeterReadingDateTime
                        ? latestReadingDb
                        : latestReadingInMemory
                };

                if (latestReading != null && record.MeterReadingDateTime <= latestReading.MeterReadingDateTime)
                {
                    result.Failed++;
                    result.Failures.Add(new MeterReadingFailureDetail
                    {
                        Reading = record,
                        Reason = "Reading is older or equal to the latest one"
                    });
                    continue;
                }


                var entity = new MeterReading
                {
                    AccountId = record.AccountId,
                    MeterReadingDateTime = record.MeterReadingDateTime,
                    MeterReadValue = record.MeterReadValue
                };

                newReadings.Add(entity);

                if (!latestReadingsInMemory.TryGetValue(record.AccountId, out var currentLatest)
                    ||
                    record.MeterReadingDateTime > currentLatest.MeterReadingDateTime)
                {
                    latestReadingsInMemory[record.AccountId] = entity;
                }
                result.Successful++;
            }
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError(ex, "Failed to parse CSV file.");
            throw new InvalidDataException("CSV file format is invalid. Please check headers and data format.", ex);
        }

        foreach (var failure in result.Failures)
        {
            _logger.LogWarning("Failed to process reading: {@Reading}, Reason: {Reason}", failure.Reading, failure.Reason);
        }

        if (newReadings.Count > 0)
        {
            await _context.MeterReadings.AddRangeAsync(newReadings, ct);
            await _context.SaveChangesAsync(ct);
        }

        return result;
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