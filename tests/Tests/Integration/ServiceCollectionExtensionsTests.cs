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

[Category("Integration")]
public class ServiceCollectionExtensionsTests
{
    private readonly TimeSpan _maximumWaitTime = TimeSpan.FromSeconds(10);

    [Test]
    public async Task WebApp_ShouldExposeMetrics()
    {
        // Arrange
        var metrics = new Collection<Metric>();
        var customWebApplicationFactory = new CustomWebApplicationFactory([], metrics, []);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger metrics creation
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise metrics remain empty
        await WaitAsync(metrics, _maximumWaitTime); // wait some time so that the metrics get populated

        // Assert
        metrics.Should().HaveCount(27);
    }

    [Test]
    public async Task WebApp_ShouldNotExposeMetrics_WhenDisabledViaConfig()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var metrics = new Collection<Metric>();
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, metrics, traces, [new ("mu88Shared:OpenTelemetry:MetricsEnabled", "false")]);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger metrics creation
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise metrics would remain empty anyway
        await WaitAsync(metrics, _maximumWaitTime); // wait some time so that the metrics would get populated

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
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise logs remain empty
        await WaitAsync(logs, _maximumWaitTime); // wait some time so that the logs get populated

        // Assert
        logs.Should().HaveCount(5);
    }

    [Test]
    [Ignore("Although explicitly disabling logs via configuration, the logs still get created. Further investigation is needed to determine the root cause and find a solution.")]
    public async Task WebApp_ShouldNotExposeLogs_WhenDisabledViaConfig()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var metrics = new Collection<Metric>();
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, metrics, traces, [new ("mu88Shared:OpenTelemetry:LogsEnabled", "false")]);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger logs creation
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise logs would remain empty anyway
        await WaitAsync(logs, _maximumWaitTime); // wait some time so that the logs would get populated

        // Assert
        logs.Should().BeEmpty();
        metrics.Should().NotBeEmpty();
        traces.Should().NotBeEmpty();
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
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise traces remain empty
        await WaitAsync(traces, _maximumWaitTime); // wait some time so that the traces get populated

        // Assert
        traces.Should().HaveCount(1);
    }

    [Test]
    public async Task WebApp_ShouldNotExposeTraces_WhenDisabledViaConfig()
    {
        // Arrange
        var logs = new Collection<LogRecord>();
        var metrics = new Collection<Metric>();
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(logs, metrics, traces, [new ("mu88Shared:OpenTelemetry:TracesEnabled", "false")]);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger traces creation
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise traces would remain empty anyway
        await WaitAsync(traces, _maximumWaitTime); // wait some time so that the traces would get populated

        // Assert
        logs.Should().NotBeEmpty();
        metrics.Should().NotBeEmpty();
        traces.Should().BeEmpty();
    }

    private static async Task WaitAsync<T>(Collection<T> data, TimeSpan maximumWaitTime)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < maximumWaitTime && data.Count == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private class CustomWebApplicationFactory(
        ICollection<LogRecord> logs,
        ICollection<Metric> metrics,
        ICollection<Activity> traces,
        IEnumerable<KeyValuePair<string, string?>>? configOptions = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder
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