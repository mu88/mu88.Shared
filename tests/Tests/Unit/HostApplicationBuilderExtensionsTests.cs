using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var returned = builder.ConfigureOpenTelemetry("my-service");

        // Assert
        returned.Should().BeSameAs(builder);

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
            ["mu88Shared:OpenTelemetry:MetricsEnabled"] = "false",
            ["mu88Shared:OpenTelemetry:TracesEnabled"] = "false",
            ["mu88Shared:OpenTelemetry:LogsEnabled"] = "false"
        });

        // Act
        builder.ConfigureOpenTelemetry("my-service");

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var options = sp.GetService<IOptions<Mu88SharedOptions>>();
        options.Should().NotBeNull();
        options.Value.OpenTelemetry.MetricsEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.TracesEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.LogsEnabled.Should().BeFalse();
    }
}