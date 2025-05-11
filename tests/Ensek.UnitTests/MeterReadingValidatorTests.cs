using Ensek.Core.Dtos;
using Ensek.Core.Interfaces;
using Ensek.Services;
using Ensek.Test.Helpers;

namespace Ensek.UnitTests;

public class MeterReadingValidatorTests : TestBase
{
    private readonly IMeterReadingValidator _validator;
    public MeterReadingValidatorTests()
    {
        _validator = new MeterReadingValidator(MeterReadingOptions);
    }
    [Fact]
    public void Invalid_AccountId_Returns_Error()
    {
        var dto = new MeterReadingDto
        {
            AccountId = -1,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("AccountId is invalid", errorMessage);
    }

    [Fact]
    public void Null_Dto_Returns_Error()
    {
        var result = _validator.IsValid(null, out string? errorMessage);

        Assert.False(result);
        Assert.Equal("DTO is null.", errorMessage);
    }

    [Fact]
    public void AccountId_Zero_Returns_Error()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 0,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.False(result);
        Assert.Contains("AccountId is invalid", errorMessage);
    }

    [Fact]
    public void MeterReadValue_Negative_Returns_Error()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = -1
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.False(result);
        Assert.Contains("MeterReadValue must be between 0 and 99999", errorMessage);
    }

    [Fact]
    public void MeterReadValue_TooHigh_Returns_Error()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 100000
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.False(result);
        Assert.Contains("MeterReadValue must be between 0 and 99999", errorMessage);
    }

    [Fact]
    public void Valid_AccountId_Passes()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 1,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void MeterReadValue_ExactMin_IsValid()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 0
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void MeterReadValue_ExactMax_IsValid()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 99999
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Invalid_DateTime_DefaultValue_Returns_Error()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = default,
            MeterReadValue = 12345
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.False(result);
        Assert.Contains("Invalid date time format", errorMessage);
    }

    [Fact]
    public void Valid_DateTime_Passes()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 12345
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void All_Valid_Fields_Returns_Success()
    {
        var dto = new MeterReadingDto
        {
            AccountId = 123,
            MeterReadingDateTime = new DateTime(2024, 04, 05),
            MeterReadValue = 54321
        };

        var result = _validator.IsValid(dto, out string? errorMessage);

        Assert.True(result);
        Assert.Null(errorMessage);
    }
}