using DotNet.Testcontainers.Containers;
using FluentAssertions;
using FluentAssertions.Web;

namespace mu88.Shared.Testing.Assertions;

/// <summary>
/// FluentAssertions-style helpers to assert that a running app/container in a system test is healthy and that its
/// logs don't contain unexpected warnings.
/// </summary>
public static class SystemTestAssertions
{
    /// <summary>
    /// Asserts that a "/healthz"-style health check response returned HTTP 200 with the body "Healthy".
    /// </summary>
    public static async Task HealthCheckShouldBeHealthyAsync(this HttpResponseMessage healthCheckResponse, CancellationToken cancellationToken)
    {
        healthCheckResponse.Should().Be200Ok();
        (await healthCheckResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Be("Healthy");
    }

    /// <summary>
    /// Asserts that the given container's stdout does not contain any "warn:"-prefixed log lines.
    /// </summary>
    public static async Task LogsShouldNotContainWarningsAsync(this IContainer container, CancellationToken cancellationToken)
    {
        (string stdout, string stderr) = await container.GetLogsAsync(ct: cancellationToken);
        Console.WriteLine($"Stderr:{Environment.NewLine}{stderr}");
        Console.WriteLine($"Stdout:{Environment.NewLine}{stdout}");
        stdout
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Should().NotContain(line => line.Contains("warn:", StringComparison.Ordinal));
    }

    /// <summary>
    /// Runs mu88.HealthCheck.dll (see mu88.Shared) inside the given container against the given health check URL
    /// and asserts it exits with code 0 (meaning the app reported healthy).
    /// </summary>
    public static async Task HealthCheckToolShouldSucceedAsync(this IContainer container, string healthCheckUrl, CancellationToken cancellationToken)
    {
        var result = await container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", healthCheckUrl], cancellationToken);
        result.ExitCode.Should().Be(0);
    }
}
