using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        customWebApplicationFactory.Services.GetRequiredService<MeterProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

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
        customWebApplicationFactory.Services.GetRequiredService<LoggerProvider>().ForceFlush();
        customWebApplicationFactory.Services.GetRequiredService<TracerProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

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
        customWebApplicationFactory.Services.GetRequiredService<LoggerProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

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
        customWebApplicationFactory.Services.GetRequiredService<TracerProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

        // Assert
        traces.Should().ContainSingle(a => a.DisplayName.Contains("/hello"));
    }

    [Test]
    public async Task WebApp_ShouldExposeHttpClientTraces()
    {
        // Arrange
        var traces = new Collection<Activity>();
        using var loopbackServer = new LoopbackHttpServer();
        var customWebApplicationFactory = new CustomWebApplicationFactory([], [], traces);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        var serveTask = loopbackServer.ServeSingleOkResponseAsync();
        (await httpClient.GetAsync($"call-external?url={Uri.EscapeDataString(loopbackServer.Url)}")).Should().Be200Ok(); // trigger outgoing HttpClient call
        await serveTask;
        customWebApplicationFactory.Services.GetRequiredService<TracerProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

        // Assert
        traces.Should().Contain(activity => activity.Source.Name == "System.Net.Http");
    }

    [Test]
    public async Task WebApp_ShouldNotExposeHealthCheckTraces()
    {
        // Arrange
        var traces = new Collection<Activity>();
        var customWebApplicationFactory = new CustomWebApplicationFactory([], [], traces);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("healthz")).Should().Be200Ok();
        await customWebApplicationFactory.DisposeAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(10000));

        // Assert
        traces.Should().NotContain(activity => activity.DisplayName.Contains("/healthz"));
    }

    [Test]
    public async Task WebApp_ShouldExposeHealthCheckMetrics()
    {
        // Arrange
        var metrics = new Collection<Metric>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(
            [],
            metrics,
            [],
            configureServices: services =>
            {
                services.AddHealthChecks().AddCheck(
                    "test-health-check",
                    () => HealthCheckResult.Healthy());
            });
        _ = customWebApplicationFactory.CreateClient();

        // Act
        var publisher = customWebApplicationFactory.Services.GetRequiredService<IHealthCheckPublisher>();
        var healthCheckService = customWebApplicationFactory.Services.GetRequiredService<HealthCheckService>();
        var meterProvider = customWebApplicationFactory.Services.GetRequiredService<MeterProvider>();
        var report = await healthCheckService.CheckHealthAsync();
        await publisher.PublishAsync(report, CancellationToken.None);
        meterProvider.ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

        // Assert
        metrics
            .Any(metric =>
                string.Equals(metric.Name, "dotnet.health_check.reports", StringComparison.Ordinal)
                || string.Equals(metric.Name, "dotnet.health_check.unhealthy_checks", StringComparison.Ordinal))
            .Should().BeTrue();
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
        customWebApplicationFactory.Services.GetRequiredService<LoggerProvider>().ForceFlush();
        customWebApplicationFactory.Services.GetRequiredService<MeterProvider>().ForceFlush();
        await customWebApplicationFactory.DisposeAsync();

        // Assert
        logs.Should().NotBeEmpty();
        metrics.Should().NotBeEmpty();
        traces.Should().BeEmpty();
    }

    private class CustomWebApplicationFactory(
        ICollection<LogRecord> logs,
        ICollection<Metric> metrics,
        ICollection<Activity> traces,
        IEnumerable<KeyValuePair<string, string?>>? configOptions = null,
        Action<IServiceCollection>? configureServices = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder
                .ConfigureServices(services =>
                {
                    var configurationManager = new ConfigurationManager();
                    configurationManager.AddInMemoryCollection(configOptions);
                    services.ConfigureOpenTelemetry("test-application", configurationManager);
                    configureServices?.Invoke(services);
                    services
                        .AddOpenTelemetry()
                        .WithMetrics(metricsBuilder => metricsBuilder.AddInMemoryExporter(metrics))
                        .WithLogging(loggingBuilder => loggingBuilder.AddInMemoryExporter(logs))
                        .WithTracing(tracingBuilder => tracingBuilder.AddInMemoryExporter(traces));
                });
    }

    /// <summary>
    ///     A minimal, real (loopback-only) HTTP server used to trigger genuine outgoing HttpClient network activity.
    ///     A mocked <see cref="System.Net.Http.HttpMessageHandler" /> (e.g. via a test double) would bypass the
    ///     framework's own <c>SocketsHttpHandler</c>, which is where the "System.Net.Http" trace activity is created,
    ///     so a real loopback call is required to reliably verify HttpClient trace instrumentation end-to-end.
    /// </summary>
    private sealed class LoopbackHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public LoopbackHttpServer()
        {
            var port = GetFreeTcpPort();
            Url = $"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}/ping/";
            _listener.Prefixes.Add(Url);
            _listener.Start();
        }

        public string Url { get; }

        public void Dispose() => _listener.Close();

        public async Task ServeSingleOkResponseAsync()
        {
            var context = await _listener.GetContextAsync();
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Close();
        }

        private static int GetFreeTcpPort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }
    }
}
