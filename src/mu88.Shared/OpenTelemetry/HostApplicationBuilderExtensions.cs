using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mu88.Shared.Settings;
using OpenTelemetry.Logs;
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
    ///     Adds and configures logs, metrics and traces for ASP.NET Core, Entity Framework Core, .NET process and .NET runtime instrumentation using OpenTelemetry.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="IHostApplicationBuilder" /> instance on which the OpenTelemetry features will be
    ///     configured.
    /// </param>
    /// <param name="serviceName">The name of the service so that it can be identified (e.g. the application name).</param>
    /// <returns>The provided <paramref name="builder" /> with configured OpenTelemetry features.</returns>
    /// <remarks>
    ///     Don't forget to set the .NET configuration parameter <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> for the OpenTelemetry
    ///     endpoint receiving the exported logs, metrics and traces.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Services.AddOptions<Mu88SharedOptions>().Bind(builder.Configuration.GetSection(Mu88SharedOptions.SectionName));
        var mu88SharedOptions = builder.Configuration.GetSection(Mu88SharedOptions.SectionName).Get<Mu88SharedOptions>() ?? new Mu88SharedOptions();

        builder.Services
               .AddOpenTelemetry()
               .ConfigureResource(c => c.AddService(serviceName));
        if (mu88SharedOptions.OpenTelemetry.LogsEnabled)
        {
            builder.Logging
                   .AddOpenTelemetry(logging =>
                   {
                       logging.IncludeFormattedMessage = true;
                       logging.IncludeScopes = true;
                   });
            builder.Services
                   .AddOpenTelemetry()
                   .WithLogging(logging => logging.AddOtlpExporter());
        }

        if (mu88SharedOptions.OpenTelemetry.MetricsEnabled)
        {
            builder.Services
                   .AddOpenTelemetry()
                   .WithMetrics(metrics =>
                   {
                       metrics
                           .AddAspNetCoreInstrumentation()
                           .AddProcessInstrumentation()
                           .AddRuntimeInstrumentation()
                           .AddOtlpExporter();
                   });
        }

        if (mu88SharedOptions.OpenTelemetry.TracesEnabled)
        {
            builder.Services
                   .AddOpenTelemetry()
                   .WithTracing(tracing =>
                   {
                       tracing
                           .AddAspNetCoreInstrumentation()
                           .AddEntityFrameworkCoreInstrumentation()
                           .AddOtlpExporter();
                   });
        }

        return builder;
    }
}