using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using mu88.Shared.OpenTelemetry;
using mu88.Shared.Settings;
using NUnit.Framework;

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
        options.Value.OpenTelemetry.OpenTelemetryMetricsEnabled.Should().BeTrue();
        options.Value.OpenTelemetry.OpenTelemetryTracesEnabled.Should().BeTrue();
        options.Value.OpenTelemetry.OpenTelemetryLogsEnabled.Should().BeTrue();
    }

    [Test]
    public void ConfigureOpenTelemetry_ShouldBindOptions_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });

        // Explicitly set the configuration values to disable all OpenTelemetry features
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["mu88Shared:OpenTelemetry:OpenTelemetryMetricsEnabled"] = "false",
            ["mu88Shared:OpenTelemetry:OpenTelemetryTracesEnabled"] = "false",
            ["mu88Shared:OpenTelemetry:OpenTelemetryLogsEnabled"] = "false"
        });

        // Act
        builder.ConfigureOpenTelemetry("my-service");

        // Assert
        using var sp = builder.Services.BuildServiceProvider();
        var options = sp.GetService<IOptions<Mu88SharedOptions>>();
        options.Should().NotBeNull();
        options.Value.OpenTelemetry.OpenTelemetryMetricsEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.OpenTelemetryTracesEnabled.Should().BeFalse();
        options.Value.OpenTelemetry.OpenTelemetryLogsEnabled.Should().BeFalse();
    }
}