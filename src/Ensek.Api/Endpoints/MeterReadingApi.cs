using Ensek.Api.Extensions;
using Ensek.Core.Interfaces;

namespace Ensek.Api.Endpoints;

public static class MeterReadingApi
{
    public static void MeterReadingModule(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/v{version:apiVersion}/meter-reading-uploads", async (IFormFile meterReadingFile, IMeterReadingService meterReadingService) =>
        {
            if (meterReadingFile == null || meterReadingFile.Length == 0)
                return Results.BadRequest("No file uploaded.");

            var result = await meterReadingService.ProcessReadingsAsync(meterReadingFile.OpenReadStream());

            return Results.Ok(new
            {
                SuccessfulReadings = result.Successful,
                FailedReadings = result.Failed,
                result.Failures
            });
        }).WithApiVersion(1).DisableAntiforgery();
    }
}