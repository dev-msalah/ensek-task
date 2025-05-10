using Ensek.Core.Results;

namespace Ensek.Core.Interfaces;

public interface IMeterReadingService
{
    Task<MeterReadingProcessingResult> ProcessReadingsAsync(Stream csvStream, CancellationToken ct = default);
}