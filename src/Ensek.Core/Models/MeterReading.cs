namespace Ensek.Core.Models;

public class MeterReading
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateTime MeterReadingDateTime { get; set; }
    public int MeterReadValue { get; set; }
    public Account? Account { get; set; }
}