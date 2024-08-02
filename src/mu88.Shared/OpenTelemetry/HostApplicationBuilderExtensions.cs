using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace mu88.Shared.OpenTelemetry;

public static class HostApplicationBuilderExtensions
{
    public static void ConfigureOpenTelemetry(this IHostApplicationBuilder builder, string serviceName)
    {
        var otlpEndpointNotSet = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (otlpEndpointNotSet)
        {
            throw new InvalidOperationException("The configuration parameter 'OTEL_EXPORTER_OTLP_ENDPOINT' must be specified.");
        }

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
                       .AddRuntimeInstrumentation();
               })
               .WithTracing(tracing =>
               {
                   tracing.AddAspNetCoreInstrumentation();
               });

        builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }
}