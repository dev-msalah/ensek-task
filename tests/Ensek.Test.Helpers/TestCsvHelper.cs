using CsvHelper;
using Ensek.Core.Dtos;
using System.Globalization;

namespace Ensek.Test.Helpers;
public static class TestCsvHelper
{
    public static Stream CreateCsvStream(IEnumerable<MeterReadingDto> dtos)
    {
        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("en-GB"), leaveOpen: true);

        csv.WriteRecords(dtos);
        writer.Flush();
        memoryStream.Position = 0;
        return memoryStream;
    }

    public static async Task<Stream> CreateCsvStreamAsync(IEnumerable<MeterReadingDto> dtos)
    {
        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, leaveOpen: true);

        await csv.WriteRecordsAsync(dtos);
        await writer.FlushAsync();
        memoryStream.Position = 0;
        return memoryStream;
    }
}