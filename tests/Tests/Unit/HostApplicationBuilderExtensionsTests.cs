using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mu88.Shared.OpenTelemetry;
using mu88.Shared.Settings;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class HostApplicationBuilderExtensionsTests
{
    [Test]
    public void ConfigureOpenTelemetry_ShouldReturnSameBuilder_And_RegisterDefaultOptions_WhenNoConfigurationProvided()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

        // Act
        builder.Services.ConfigureOpenTelemetry("my-service", builder.Configuration);

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var options = sp.GetService<IOptions<Mu88SharedOptions>>();
        options.Should().NotBeNull();
        options.Value.OpenTelemetry.MetricsEnabled.Should().BeTrue();
        options.Value.OpenTelemetry.TracesEnabled.Should().BeTrue();
        options.Value.OpenTelemetry.LogsEnabled.Should().BeTrue();
    }

    [Test]
    public void ConfigureOpenTelemetry_ShouldBindOptions_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

        // Explicitly set the configuration values to disable all OpenTelemetry features
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["mu88Shared:OpenTelemetry:MetricsEnabled"] = "false", ["mu88Shared:OpenTelemetry:TracesEnabled"] = "false", ["mu88Shared:OpenTelemetry:LogsEnabled"] = "false"
        });

        // Act
        builder.Services.ConfigureOpenTelemetry("my-service", builder.Configuration);

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var options = sp.GetService<IOptions<Mu88SharedOptions>>();
        options.Should().NotBeNull();
        options.Value.OpenTelemetry.MetricsEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.TracesEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.LogsEnabled.Should().BeFalse();
    }

    [Test]
    public void ConfigureOpenTelemetry_ShouldSuppressOtlpExporterHttpClientLogNoise_WhenAllFeaturesEnabled()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

        // Act
        builder.Services.ConfigureOpenTelemetry("my-service", builder.Configuration);

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var rules = sp.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;
        rules.Should().Contain(rule => rule.CategoryName == "System.Net.Http.HttpClient.OtlpLogExporter" && rule.LogLevel == LogLevel.Warning);
        rules.Should().Contain(rule => rule.CategoryName == "System.Net.Http.HttpClient.OtlpMetricExporter" && rule.LogLevel == LogLevel.Warning);
        rules.Should().Contain(rule => rule.CategoryName == "System.Net.Http.HttpClient.OtlpTraceExporter" && rule.LogLevel == LogLevel.Warning);
    }

    [Test]
    public void ConfigureOpenTelemetry_ShouldNotSuppressOtlpExporterHttpClientLogNoise_WhenAllFeaturesDisabled()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

        // Explicitly set the configuration values to disable all OpenTelemetry features
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["mu88Shared:OpenTelemetry:MetricsEnabled"] = "false", ["mu88Shared:OpenTelemetry:TracesEnabled"] = "false", ["mu88Shared:OpenTelemetry:LogsEnabled"] = "false"
        });

        // Act
        builder.Services.ConfigureOpenTelemetry("my-service", builder.Configuration);

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var rules = sp.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;
        rules.Should().NotContain(rule => rule.CategoryName != null && rule.CategoryName.StartsWith("System.Net.Http.HttpClient.Otlp", StringComparison.Ordinal));
    }
}
