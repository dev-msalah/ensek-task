using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;

namespace Ensek.Services;

public class MeterReadingValidator : IMeterReadingValidator
{
    public bool IsValid(MeterReadingDto dto, out string? errorMessage)
    {
        errorMessage = null;

        if (dto == null)
        {
            errorMessage = "DTO is null.";
            return false;
        }

        if (dto.AccountId <= 0)
        {
            errorMessage = "AccountId is invalid.";
            return false;
        }

        // TODO: move the min and max meter read value to config file or setting table later on 
        if (dto.MeterReadValue < 0 || dto.MeterReadValue > 99999)
        {
            errorMessage = "MeterReadValue must be between 0 and 99999.";
            return false;
        }

        if (dto.MeterReadingDateTime == default)
        {
            errorMessage = "Invalid date time format.";
            return false;
        }

        return true;
    }
}