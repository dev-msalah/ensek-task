using Asp.Versioning.Builder;
using Asp.Versioning;

namespace Ensek.Api.Extensions;

public static class EndpointConventionBuilderExtensions
{
    private static ApiVersionSet? _apiVersionSet;

    public static void InitializeApiVersionSet(IEndpointRouteBuilder routeBuilder)
    {
        _apiVersionSet = routeBuilder.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .HasDeprecatedApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();
    }

    public static TBuilder WithApiVersion<TBuilder>(this TBuilder builder, decimal versionNumber)
        where TBuilder : notnull, IEndpointConventionBuilder
    {
        if (_apiVersionSet == null)
            throw new InvalidOperationException("ApiVersionSet is not initialized. Call InitializeApiVersionSet first.");

        var majorVersion = (int)Math.Floor(versionNumber);
        var minorVersion = (int)((versionNumber - majorVersion) * 10); 
        var apiVersion = new ApiVersion(majorVersion, minorVersion);

        builder.WithApiVersionSet(_apiVersionSet).MapToApiVersion(apiVersion);
        return builder;
    }
}