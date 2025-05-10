namespace Ensek.Core.Dtos;
public class MeterReadingDto
{
    public int AccountId { get; set; }
    public DateTime MeterReadingDateTime { get; set; }
    public int MeterReadValue { get; set; }
}