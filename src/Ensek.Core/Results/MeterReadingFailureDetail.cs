using Ensek.Core.Dtos;

namespace Ensek.Core.Results;
public class MeterReadingFailureDetail
{
    public MeterReadingDto Reading { get; set; } = null!;
    public string Reason { get; set; } = null!;
}