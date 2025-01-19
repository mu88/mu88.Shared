using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using mu88.HealthCheck;
using RichardSzalay.MockHttp;
using System.Net;

namespace Tests.Integration;

[TestFixture]
[Category("Integration")]
public class HealthCheckerTests
{
    [Test]
    public async Task HealthChecker_ShouldIndicateSuccess_WhenAppIsHealthy()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<Program>();
        using var httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        var response = await httpClient.GetAsync("healthz");
        var healthCheckerResult = await healthChecker.CheckHealthAsync(["healthz"]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthCheckerResult.Should().Be(0);
    }

    [Test]
    public async Task HealthChecker_ShouldIndicateFailure_WhenAppIsUnhealthy()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://localhost:8080/healthz").Respond("text/plain", "Unhealthy");
        var healthChecker = new HealthChecker(mockHttp.ToHttpClient());

        // Act
        var healthCheckerResult = await healthChecker.CheckHealthAsync(["http://localhost:8080/healthz"]);

        // Assert
        healthCheckerResult.Should().Be(1);
    }

    [Test]
    public async Task HealthChecker_ShouldIndicateFailure_WhenWrongHealthCheckEndpointIsUsed()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<Program>();
        using var httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        var response = await httpClient.GetAsync("healthz");
        var healthCheckerResult = await healthChecker.CheckHealthAsync(["invalidHealthCheckEndpoint"]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthCheckerResult.Should().Be(1);
    }

    [Test]
    public async Task HealthChecker_ShouldThrowArgumentException_WhenUriIsInvalid()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<Program>();
        using var httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        var response = await httpClient.GetAsync("healthz");
        Func<Task> act = async () => await healthChecker.CheckHealthAsync([]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("A valid URI must be given as first argument (Parameter 'args')");
    }
}