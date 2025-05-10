using Ensek.Core.Dtos;

namespace Ensek.Core.Interfaces;
public interface IMeterReadingValidator
{
    bool IsValid(MeterReadingDto dto, out string? errorMessage);
}