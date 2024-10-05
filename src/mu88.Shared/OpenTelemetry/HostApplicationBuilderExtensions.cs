using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace mu88.Shared.OpenTelemetry;

/// <summary>
///     Extensions for an <see cref="IHostApplicationBuilder" /> instance.
/// </summary>
// ReSharper disable once UnusedType.Global - reviewed mu88: public API
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    ///     Adds and configures the following OpenTelemetry features:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Metrics for ASP.NET Core, .NET process and .NET runtime.</description>
    ///         </item>
    ///         <item>
    ///             <description>Traces for ASP.NET Core and Entity Framework Core.</description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="IHostApplicationBuilder" /> instance on which the OpenTelemetry features will be
    ///     configured.
    /// </param>
    /// <param name="serviceName">The name of the service so that it can be identified (e.g. the application name).</param>
    /// <returns>The provided <paramref name="builder" /> with configured OpenTelemetry features.</returns>
    /// <remarks>
    ///     Don't forget to set the .NET configuration parameter <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> for the OpenTelemetry
    ///     endpoint receiving the exported metrics and traces.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

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
               })
               .WithTracing(tracing =>
               {
                   tracing
                       .AddAspNetCoreInstrumentation()
                       .AddEntityFrameworkCoreInstrumentation();
               });

        return builder;
    }
}