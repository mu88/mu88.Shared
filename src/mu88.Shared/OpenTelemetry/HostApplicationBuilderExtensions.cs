using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace mu88.Shared.OpenTelemetry;

/// <summary>
///     Extensions for an <see cref="IHostApplicationBuilder" /> instance.
/// </summary>
// ReSharper disable once UnusedType.Global - reviewed mu88: public API
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    ///     Adds and configures metrics for ASP.NET Core, .NET process and .NET runtime instrumentation using OpenTelemetry.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="IHostApplicationBuilder" /> instance on which the OpenTelemetry features will be
    ///     configured.
    /// </param>
    /// <param name="serviceName">The name of the service so that it can be identified (e.g. the application name).</param>
    /// <returns>The provided <paramref name="builder" /> with configured OpenTelemetry features.</returns>
    /// <remarks>
    ///     Don't forget to set the .NET configuration parameter <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> for the OpenTelemetry
    ///     endpoint receiving the exported metrics.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IHostApplicationBuilder ConfigureOpenTelemetryMetrics(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Services
               .AddOpenTelemetry()
               .UseOtlpExporter()
               .ConfigureResource(c => c.AddService(serviceName))
               .WithMetrics(metrics =>
               {
                   metrics
                       .AddAspNetCoreInstrumentation()
                       .AddProcessInstrumentation()
                       .AddRuntimeInstrumentation();
               });

        return builder;
    }
}