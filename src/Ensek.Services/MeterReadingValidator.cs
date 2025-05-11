using Ensek.Core.Configuration;
using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;

namespace Ensek.Services;

public class MeterReadingValidator : IMeterReadingValidator
{
    private readonly MeterReadingConfig _config;

    public MeterReadingValidator(MeterReadingConfig config)
    {
        _config = config;
    }

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

        if (dto.MeterReadValue < _config.MinMeterReadingValue ||
           dto.MeterReadValue > _config.MaxMeterReadingValue)
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