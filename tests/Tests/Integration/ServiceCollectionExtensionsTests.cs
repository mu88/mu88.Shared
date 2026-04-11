using System.Collections.ObjectModel;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mu88.Shared.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Tests.Integration;

[TestFixture]
[Category("Integration")]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task WebApp_ShouldExposeMetrics()
    {
        // Arrange
        var metrics = new Collection<Metric>();
        var customWebApplicationFactory = new CustomWebApplicationFactory([], metrics, []);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger metrics creation
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        metrics.Should().Contain(m => m.Name == "http.server.request.duration");
        metrics.Should().Contain(m => m.Name == "process.cpu.time");
        metrics.Should().Contain(m => m.Name == "dotnet.gc.heap.total_allocated");
    }

    [Test]
    public async Task WebApp_ShouldNotExposeMetrics_WhenDisabledViaConfig()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var metrics = new Collection<Metric>();
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, metrics, traces, [new("mu88Shared:OpenTelemetry:MetricsEnabled", "false")]);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger metrics creation
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        logs.Should().NotBeEmpty();
        metrics.Should().BeEmpty();
        traces.Should().NotBeEmpty();
    }

    [Test]
    public async Task WebApp_ShouldExposeLogs()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, [], []);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger logs creation
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        logs.Should().NotBeEmpty();
        logs.Should().Contain(log => log.FormattedMessage != null && log.FormattedMessage.Contains("Saying hello"));
    }

    [Test]
    public async Task WebApp_ShouldExposeTraces()
    {
        // Arrange
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory([], [], traces);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger traces creation
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        traces.Should().ContainSingle(a => a.DisplayName.Contains("/hello"));
    }

    [Test]
    public async Task WebApp_ShouldNotExposeTraces_WhenDisabledViaConfig()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var metrics = new Collection<Metric>();
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, metrics, traces, [new("mu88Shared:OpenTelemetry:TracesEnabled", "false")]);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger traces creation
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        logs.Should().NotBeEmpty();
        metrics.Should().NotBeEmpty();
        traces.Should().BeEmpty();
    }

    private class CustomWebApplicationFactory(
        ICollection<LogRecord> logs,
        ICollection<Metric> metrics,
        ICollection<Activity> traces,
        IEnumerable<KeyValuePair<string, string?>>? configOptions = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder
                .ConfigureServices(services =>
                {
                    var configurationManager = new ConfigurationManager();
                    configurationManager.AddInMemoryCollection(configOptions);
                    services.ConfigureOpenTelemetry("test-application", configurationManager);
                    services
                        .AddOpenTelemetry()
                        .WithMetrics(metricsBuilder => metricsBuilder.AddInMemoryExporter(metrics))
                        .WithLogging(loggingBuilder => loggingBuilder.AddInMemoryExporter(logs))
                        .WithTracing(tracingBuilder => tracingBuilder.AddInMemoryExporter(traces));
                });
    }
}
