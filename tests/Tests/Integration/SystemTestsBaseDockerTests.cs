using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using mu88.Shared.Testing.Docker;
using mu88.Shared.Testing.SystemTests;

namespace Tests.Integration;

/// <summary>
/// Dogfoods <see cref="SystemTestsBase"/>/<see cref="DockerImageBuilder"/>/the assertion helpers against a real
/// Testcontainers container, built from a throwaway copy of DummyAspNetCoreProjectViaNuGet that references
/// mu88.Shared via an actual local NuGet package - exactly like a real external consumer (e.g. RaspiFanController)
/// would - rather than a ProjectReference, see <see cref="NuGetTestProjectHelper"/>. This is the package's own
/// proof that its Docker/Testcontainers-dependent code actually works end-to-end, instead of relying solely on
/// downstream consumers' System tests. Categorized "Integration" (not "System") so it runs as part of the
/// coverage-collected Unit+Integration test run.
/// </summary>
[Category("Integration")]
public class SystemTestsBaseDockerTests : SystemTestsBase
{
    private const string ContainerRepository = "mu88-shared-testing-dummy-nuget";
    private DirectoryInfo _tempDirectory = null!;
    private DirectoryInfo _tempNugetDirectory = null!;
    private DirectoryInfo _tempTestProjectDirectory = null!;

    protected override string SubPath => string.Empty;

    [SetUp]
    public void SetupTempDirectories()
    {
        _tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_Testing_SystemTestsBaseDockerTests_");
        _tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
        _tempNugetDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "NuGet"));
    }

    [TearDown]
    public void CleanupTempDirectories() => _tempDirectory.Delete(true);

    [Test]
    public async Task AppRunningInDocker_ShouldBeHealthyAndServeHomePage()
    {
        // Arrange
        var nugetVersion = DockerImageBuilder.GenerateContainerImageTag();
        NuGetTestProjectHelper.CopyTestProject(_tempTestProjectDirectory);
        await NuGetTestProjectHelper.BuildNuGetPackageAsync(_tempNugetDirectory, nugetVersion, CancellationToken);
        await NuGetTestProjectHelper.AddNuGetPackageToTestProjectAsync(_tempNugetDirectory, _tempTestProjectDirectory, nugetVersion, CancellationToken);
        var projectFilePath = NuGetTestProjectHelper.GetTestProjectFilePath(_tempTestProjectDirectory);
        var containerImageTag = DockerImageBuilder.GenerateContainerImageTag();
        await DockerImageBuilder.BuildAsync(
            projectFilePath,
            containerImageTag,
            ContainerRepository,
            _tempTestProjectDirectory.FullName,
            CancellationToken,
            ["-p:PublishRegularContainer=true", "-p:IsRelease=true"]);
        Container = await StartAppInContainerAsync(containerImageTag, CancellationToken);

        // Assert
        await LogsShouldNotContainWarningsAsync(CancellationToken);
        await HealthCheckShouldSucceedAsync(CancellationToken);
        await AppShouldRunAsync(CancellationToken, "Dummy App");
    }

    private static async Task<IContainer> StartAppInContainerAsync(string containerImageTag, CancellationToken cancellationToken)
    {
        var container = new ContainerBuilder($"{ContainerRepository}:{containerImageTag}")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/healthz").ForPort(8080)))
            .Build();
        await container.StartAsync(cancellationToken);

        return container;
    }
}
