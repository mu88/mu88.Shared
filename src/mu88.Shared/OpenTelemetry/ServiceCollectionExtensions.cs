using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mu88.Shared.Settings;
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
    /// <returns>The provided <paramref name="services" /> with configured OpenTelemetry features.</returns>
    /// <remarks>
    ///     Don't forget to set the .NET configuration parameter <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> for the OpenTelemetry
    ///     endpoint receiving the exported logs, metrics and traces.
    /// </remarks>
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services, string serviceName, IConfigurationManager configuration)
    {
        services.AddOptions<Mu88SharedOptions>().Bind(configuration.GetSection(Mu88SharedOptions.SectionName));
        var mu88SharedOptions = configuration.GetSection(Mu88SharedOptions.SectionName).Get<Mu88SharedOptions>() ?? new Mu88SharedOptions();

        services
               .AddOpenTelemetry()
               .ConfigureResource(builder => builder.AddService(serviceName));
        if (mu88SharedOptions.OpenTelemetry.LogsEnabled)
        {
            services
                .AddOpenTelemetry()
                .WithLogging(
                    loggingBuilder => loggingBuilder.AddOtlpExporter(),
                    loggingOptions =>
                    {
                        loggingOptions.IncludeFormattedMessage = true;
                        loggingOptions.IncludeScopes = true;
                    });
        }

        if (mu88SharedOptions.OpenTelemetry.MetricsEnabled)
        {
            services
                   .AddOpenTelemetry()
                   .WithMetrics(metricsBuilder =>
                   {
                       metricsBuilder
                           .AddAspNetCoreInstrumentation()
                           .AddProcessInstrumentation()
                           .AddRuntimeInstrumentation()
                           .AddOtlpExporter();
                   });
        }

        if (mu88SharedOptions.OpenTelemetry.TracesEnabled)
        {
            services
                   .AddOpenTelemetry()
                   .WithTracing(tracingBuilder =>
                   {
                       tracingBuilder
                           .AddAspNetCoreInstrumentation()
                           .AddEntityFrameworkCoreInstrumentation()
                           .AddOtlpExporter();
                   });
        }

        return services;
    }
}