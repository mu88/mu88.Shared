using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using FluentAssertions.Web;
using mu88.Shared.Testing.Assertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace mu88.Shared.Testing.SystemTests;

/// <summary>
/// Base class for NUnit-based system tests that build and start an app inside a Testcontainers-managed Docker
/// container. Provides the per-test Docker-client/cancellation-token lifecycle (SetUp/TearDown), base-address
/// resolution for the app's path base, and symmetric assertion wrappers
/// (<see cref="LogsShouldNotContainWarningsAsync"/>, <see cref="HealthCheckShouldSucceedAsync"/>,
/// <see cref="AppShouldRunAsync(CancellationToken)"/>) that all read from this fixture's own
/// <see cref="Container"/>/<see cref="HttpClient"/> state - no ambient/static context involved. This is the one
/// NUnit-specific part of mu88.Shared.Testing; everything else in this package is NUnit/xUnit-agnostic.
/// Derived classes must apply their own NUnit <see cref="CategoryAttribute"/> (typically "System") - this base
/// class deliberately does not impose one, since not every derived test is a heavyweight app-container System
/// test (e.g. this package's own dogfooding test is categorized "Integration" instead).
/// </summary>
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP006:Implement IDisposable", Justification = "NUnit test fixture; HttpClient/DockerClient/CancellationTokenSource are disposed via [TearDown], not an IDisposable implementation.")]
public abstract class SystemTestsBase
{
    private IContainer? _container;
    private HttpClient? _httpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private DockerClient? _dockerClient;

    /// <summary>
    /// The app's path base (e.g. "/cool"), used to build both the externally mapped base address and the
    /// in-container health check URL. Return an empty string if the app has no path base.
    /// </summary>
    protected abstract string SubPath { get; }

    /// <summary>
    /// Maximum duration of a single test run before its <see cref="CancellationToken"/> is cancelled. Defaults to
    /// one minute; override for apps whose system tests need more time (e.g. a slower startup sequence).
    /// </summary>
    protected virtual TimeSpan Timeout => TimeSpan.FromMinutes(1);

    /// <summary>
    /// The app's main container, set by the derived test once it has been built and started. Setting this property
    /// automatically (re-)builds <see cref="HttpClient"/> from the container's externally mapped base address. The
    /// container is automatically stopped, disposed and removed in the teardown if the test passed. Reading this
    /// property before it has been set throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "A fresh HttpClient is intentionally (re-)created whenever the app's container is (re-)assigned, since its base address depends on the container's mapped port; the previous instance is disposed first.")]
    protected IContainer Container
    {
        get => _container ?? throw new InvalidOperationException($"{nameof(Container)} must be set (typically right after building and starting the app's container) before calling this method.");
        set
        {
            _container = value;
            _httpClient?.Dispose();
            _httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(value) };
        }
    }

    /// <summary>
    /// HTTP client targeting <see cref="Container"/>'s externally mapped base address (host + port + <see cref="SubPath"/>),
    /// built automatically as soon as <see cref="Container"/> is set. Used internally by
    /// <see cref="HealthCheckShouldSucceedAsync"/> and <see cref="AppShouldRunAsync(CancellationToken)"/>. Reading
    /// this property before <see cref="Container"/> has been set throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    protected HttpClient HttpClient =>
        _httpClient ?? throw new InvalidOperationException($"{nameof(Container)} must be set (which also builds {nameof(HttpClient)}) before calling this method.");

    /// <summary>
    /// Token of the per-test <see cref="System.Threading.CancellationTokenSource"/> created in <see cref="Setup"/>,
    /// to be used for all Testcontainers/HTTP calls made by the derived test.
    /// </summary>
    protected CancellationToken CancellationToken => CancellationTokenSourceInstance.Token;

    private CancellationTokenSource CancellationTokenSourceInstance =>
        _cancellationTokenSource ?? throw new InvalidOperationException($"{nameof(Setup)} must run before {nameof(CancellationToken)} can be used.");

    private DockerClient DockerClient =>
        _dockerClient ?? throw new InvalidOperationException($"{nameof(Setup)} must run before the Docker client can be used.");

    [SetUp]
    public void Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource(Timeout);
        _dockerClient = new DockerClientBuilder().Build();
    }

    [TearDown]
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "Container is set by the derived test itself (via the protected setter), not injected from outside; this class legitimately owns its cleanup.")]
    public async Task TeardownAsync()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
        {
            return; // no need to clean up on GitHub Actions runners
        }

        // If the test passed, clean up the container and image. Otherwise, keep them for investigation.
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed)
        {
            if (_container is not null)
            {
                await Container.StopAsync(CancellationToken);
                await Container.DisposeAsync();
                await DockerClient.Images.DeleteImageAsync(Container.Image.FullName, new ImageDeleteParameters { Force = true }, CancellationToken);
            }

            await CleanupAdditionalResourcesAsync(CancellationToken);
        }

        _httpClient?.Dispose();
        _dockerClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// Asserts that <see cref="Container"/>'s stdout does not contain any "warn:"-prefixed log lines. Thin wrapper
    /// around <see cref="SystemTestAssertions.LogsShouldNotContainWarningsAsync"/>, kept symmetric with
    /// <see cref="HealthCheckShouldSucceedAsync"/> and <see cref="AppShouldRunAsync(CancellationToken)"/>.
    /// </summary>
    protected Task LogsShouldNotContainWarningsAsync(CancellationToken cancellationToken) => Container.LogsShouldNotContainWarningsAsync(cancellationToken);

    /// <summary>
    /// Combines both health check mechanisms into one call: asserts that the externally reachable "healthz"
    /// endpoint returns HTTP 200/"Healthy" (via <see cref="HttpClient"/>), and that mu88.HealthCheck.dll run
    /// from inside the container against its own internal "healthz" endpoint exits successfully.
    /// </summary>
    protected async Task HealthCheckShouldSucceedAsync(CancellationToken cancellationToken, ushort internalPort = 8080)
    {
        using var healthCheckResponse = await HttpClient.GetAsync("healthz", cancellationToken);
        await healthCheckResponse.HealthCheckShouldBeHealthyAsync(cancellationToken);
        await Container.HealthCheckToolShouldSucceedAsync($"http://127.0.0.1:{internalPort.ToString(CultureInfo.InvariantCulture)}{SubPath}/healthz", cancellationToken);
    }

    /// <summary>
    /// Fetches the app's root/home response via <see cref="HttpClient"/> and asserts HTTP 200, plus (if
    /// <paramref name="expectedHomePageTitle"/> is given) that the response body contains that title.
    /// </summary>
    protected async Task AppShouldRunAsync(CancellationToken cancellationToken, string? expectedHomePageTitle = null)
    {
        using var appResponse = await HttpClient.GetAsync("/", cancellationToken);
        appResponse.Should().Be200Ok();

        if (expectedHomePageTitle is not null)
        {
            var content = await appResponse.Content.ReadAsStringAsync(cancellationToken);
            content.Should().Contain($"<title>{expectedHomePageTitle}</title>");
        }
    }

    /// <summary>
    /// Builds the app's externally reachable base address (host + mapped port + <see cref="SubPath"/>) for the
    /// given container.
    /// </summary>
    protected Uri GetAppBaseAddress(IContainer container, ushort containerPort = 8080) =>
        BuildAppBaseAddress(container.Hostname, container.GetMappedPublicPort(containerPort));

    /// <summary>
    /// Builds the app's externally reachable base address from an already-resolved host and mapped port, appending
    /// <see cref="SubPath"/>. Extracted from <see cref="GetAppBaseAddress(IContainer,ushort)"/> so the
    /// address-formatting logic can be unit-tested without a live container.
    /// </summary>
    protected Uri BuildAppBaseAddress(string hostname, int mappedPort) => new($"http://{hostname}:{mappedPort.ToString(CultureInfo.InvariantCulture)}{SubPath}");

    /// <summary>
    /// Overridable hook for derived test classes that need to clean up additional Testcontainers resources (e.g.
    /// an extra network or helper container) beyond the main <see cref="Container"/>. Called from the teardown only
    /// if the test passed. Defaults to a no-op.
    /// </summary>
    protected virtual Task CleanupAdditionalResourcesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
