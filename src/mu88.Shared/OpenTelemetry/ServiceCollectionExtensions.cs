using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mu88.Shared.Settings;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace mu88.Shared.OpenTelemetry;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds and configures logs, metrics and traces for ASP.NET Core, Entity Framework Core, .NET process and .NET runtime instrumentation using OpenTelemetry.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> instance on which the OpenTelemetry features will be
    ///     configured.
    /// </param>
    /// <param name="serviceName">The name of the service so that it can be identified (e.g. the application name).</param>
    /// <param name="configuration">The configuration instance from which the OpenTelemetry settings will be read.</param>
    /// <param name="serviceVersion">
    ///     The version of the service (e.g. the app's informational version) to be published as the
    ///     <c>service.version</c> resource attribute. Optional; when omitted, no version attribute is added.
    /// </param>
    /// <returns>The provided <paramref name="services" /> with configured OpenTelemetry features.</returns>
    /// <remarks>
    ///     Don't forget to set the .NET configuration parameter <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> for the OpenTelemetry
    ///     endpoint receiving the exported logs, metrics and traces.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IServiceCollection ConfigureOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        IConfigurationManager configuration,
        string? serviceVersion = null)
    {
        services.AddOptions<Mu88SharedOptions>().Bind(configuration.GetSection(Mu88SharedOptions.SectionName));
        var mu88SharedOptions = configuration.GetSection(Mu88SharedOptions.SectionName).Get<Mu88SharedOptions>() ?? new Mu88SharedOptions();

        var otelBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(builder => builder.AddService(serviceName, serviceVersion: serviceVersion));

        // Note: setting LogsEnabled=false disables the OTLP log exporter only.
        // Other log exporters (e.g. in-memory, console) that are added separately are unaffected.
        if (mu88SharedOptions.OpenTelemetry.LogsEnabled)
        {
            otelBuilder.WithLogging(
                loggingBuilder => loggingBuilder.AddOtlpExporter(),
                loggingOptions =>
                {
                    loggingOptions.IncludeFormattedMessage = true;
                    loggingOptions.IncludeScopes = true;
                });

            services.SuppressOtlpExporterHttpClientLogNoise("OtlpLogExporter");
        }

        if (mu88SharedOptions.OpenTelemetry.MetricsEnabled)
        {
            services.AddTelemetryHealthCheckPublisher();

            otelBuilder.WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddMeter("Microsoft.Extensions.Diagnostics.HealthChecks")
                    .AddProcessInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter();
            });

            services.SuppressOtlpExporterHttpClientLogNoise("OtlpMetricExporter");
        }

        if (mu88SharedOptions.OpenTelemetry.TracesEnabled)
        {
            services.Configure<AspNetCoreTraceInstrumentationOptions>(
                options => options.Filter = httpContext => httpContext.Request.Path != "/healthz");

            otelBuilder.WithTracing(tracingBuilder =>
            {
                tracingBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter();
            });

            services.SuppressOtlpExporterHttpClientLogNoise("OtlpTraceExporter");
        }

        return services;
    }

    /// <summary>
    ///     When using the OTLP exporter's HTTP/protobuf protocol, OpenTelemetry resolves the exporter's HttpClient via
    ///     IHttpClientFactory under a well-known name (e.g. "OtlpTraceExporter"). By default, that
    ///     IHttpClientFactory-created HttpClient logs its own request/response lifecycle under the ILogger category
    ///     "System.Net.Http.HttpClient.{name}.*", which creates noisy, low-value log entries about the export
    ///     mechanism itself. This suppresses that specific category down to Warning, leaving actual errors visible.
    /// </summary>
    private static void SuppressOtlpExporterHttpClientLogNoise(this IServiceCollection services, string otlpExporterHttpClientName) =>
        services.AddLogging(logging => logging.AddFilter($"System.Net.Http.HttpClient.{otlpExporterHttpClientName}", LogLevel.Warning));
}
