using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace mu88.Shared.OpenTelemetry;

// ReSharper disable once UnusedType.Global - reviewed mu88: public API
public static class HostApplicationBuilderExtensions
{
    // ReSharper disable once UnusedMember.Global - reviewed mu88: public API
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder, string serviceName, ILogger? logger = null)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services
               .AddOpenTelemetry()
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

        var otlpEndpointIsSet = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (otlpEndpointIsSet)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        else
        {
            if (logger == null)
            {
                using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
                logger = loggerFactory.CreateLogger(nameof(HostApplicationBuilderExtensions));
            }

            logger.LogWarning("The OpenTelemetry endpoint configuration parameter 'OTEL_EXPORTER_OTLP_ENDPOINT' is not set");
        }

        return builder;
    }
}