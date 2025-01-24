using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Tests.Integration;

[Category("Integration")]
public class HostApplicationBuilderExtensionsTests
{
    [Test]
    public async Task WebApp_ShouldExposeMetrics()
    {
        // Arrange
        var metrics = new List<Metric>();
        var customWebApplicationFactory = new CustomWebApplicationFactory(metrics);
        using var httpClient = customWebApplicationFactory.CreateClient();

        // Act
        (await httpClient.GetAsync("hello")).Should().Be200Ok(); // trigger metrics creation
        await customWebApplicationFactory.DisposeAsync(); // must be disposed, otherwise metrics remain empty
        await WaitAsync(metrics, TimeSpan.FromSeconds(30)); // wait some time so that the metrics get populated

        // Assert
        metrics.Should().HaveCount(27);
    }

    private static async Task WaitAsync(List<Metric> metrics, TimeSpan maximumWaitTime)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < maximumWaitTime && metrics.Count == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly List<Metric> _metrics;

        /// <inheritdoc />
        public CustomWebApplicationFactory(List<Metric> metrics) => _metrics = metrics;

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder
                .ConfigureServices(services => services
                                               .AddOpenTelemetry()
                                               .WithMetrics(metrics => metrics.AddInMemoryExporter(_metrics)));
    }
}