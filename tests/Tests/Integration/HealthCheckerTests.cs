using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using mu88.HealthCheck;

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
        using HttpClient httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        HttpResponseMessage response = await httpClient.GetAsync("healthz");
        var healthCheckerResult = await healthChecker.CheckHealthAsync(["healthz"]);

        // Assert
        response.Should().BeSuccessful();
        healthCheckerResult.Should().Be(0);
    }

    [Test]
    public async Task HealthChecker_ShouldIndicateFailure_WhenAppIsUnhealthy()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<Program>();
        using HttpClient httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        HttpResponseMessage response = await httpClient.GetAsync("healthz");
        var healthCheckerResult = await healthChecker.CheckHealthAsync(["unhealthy"]);

        // Assert
        response.Should().BeSuccessful();
        healthCheckerResult.Should().Be(1);
    }

    [Test]
    public async Task HealthChecker_ShouldThrowArgumentException_WhenUriIsInvalid()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<Program>();
        using HttpClient httpClient = webApplicationFactory.CreateClient();
        var healthChecker = new HealthChecker(httpClient);

        // Act
        HttpResponseMessage response = await httpClient.GetAsync("healthz");
        Func<Task> act = async () => await healthChecker.CheckHealthAsync([]);

        // Assert
        response.Should().BeSuccessful();
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("A valid URI must be given as first argument (Parameter 'args')");
    }
}