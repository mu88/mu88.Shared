using System.Net;
using FluentAssertions;
using mu88.Shared.Testing.Assertions;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class SystemTestAssertionsTests
{
    [Test]
    public async Task HealthCheckShouldBeHealthyAsync_ShouldNotThrow_WhenResponseIs200WithHealthyBody()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Healthy") };

        // Act & Assert
        await response.HealthCheckShouldBeHealthyAsync(CancellationToken.None);
    }

    [Test]
    public async Task HealthCheckShouldBeHealthyAsync_ShouldThrow_WhenResponseIsNot200()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("Unhealthy") };

        // Act
        Func<Task> act = () => response.HealthCheckShouldBeHealthyAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
