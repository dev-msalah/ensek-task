namespace Ensek.Core.Results;
public class MeterReadingProcessingResult
{
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<MeterReadingFailureDetail> Failures { get; set; } = new();
}