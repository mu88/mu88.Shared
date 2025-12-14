using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    private CancellationToken _cancellationToken;
    private DirectoryInfo _tempDirectory;
    private DockerClient _dockerClient;
    private DirectoryInfo _tempTestProjectDirectory;
    private DirectoryInfo _tempNuGetDirectory;
    private string _tempVersion;

    [SetUp]
    public void Setup()
    {
        _cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        _tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
        _tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "NuGet"));
        _tempVersion = GenerateVersion();
    }

    [TearDown]
    public void Teardown()
    {
        _tempDirectory.Delete(true);
        _dockerClient.Dispose();
    }

    [Test]
    public async Task PublishContainer_ShouldPublishRegularContainer()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishRegularContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        outputLines.Should().ContainMatch($"*Publishing regular container image with tags: {_tempVersion};latest");
        (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken)).Should()
                                                                                                    .Contain(image => image.RepoTags != null &&
                                                                                                        image.RepoTags.Contains($"me/test:{_tempVersion}") &&
                                                                                                        image.RepoTags.Contains("me/test:latest"));
    }

    [Test]
    public async Task PublishContainer_ShouldPublishChiseledContainer()
    {
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {_tempVersion}-chiseled;latest-chiseled");
        (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken)).Should()
                                                                                                    .Contain(image => image.RepoTags != null &&
                                                                                                        image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled") &&
                                                                                                        image.RepoTags.Contains("me/test:latest-chiseled"));
    }

    [Test]
    public async Task PublishContainer_ShouldPublishChiseledContainerWithExtra()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }, { "InvariantGlobalization", "false" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {_tempVersion}-chiseled;latest-chiseled");
        outputLines.Should().ContainMatch("*Using container base image: mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled-extra");
        (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken)).Should()
                                                                                                    .Contain(image => image.RepoTags != null &&
                                                                                                        image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled") &&
                                                                                                        image.RepoTags.Contains("me/test:latest-chiseled"));
    }

    [Test]
    public async Task PublishContainer_ShouldPublishRegularAndChiseledContainer()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "PublishRegularContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        outputLines.Should().ContainMatch($"*Publishing regular container image with tags: {_tempVersion};latest");
        outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {_tempVersion}-chiseled;latest-chiseled");
        outputLines.Should().ContainMatch("*Using container base image: mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled");
        var dockerImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken);
        dockerImages.Should()
                    .Contain(image => image.RepoTags != null &&
                                      image.RepoTags.Contains($"me/test:{_tempVersion}") &&
                                      image.RepoTags.Contains("me/test:latest"));
        dockerImages.Should()
                    .Contain(image => image.RepoTags != null &&
                                      image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled") &&
                                      image.RepoTags.Contains("me/test:latest-chiseled"));
    }

    [Test]
    public async Task PublishContainer_ShouldSetContainerMetadata()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishRegularContainer", "true" },
            { "ReleaseVersion", _tempVersion },
            { "IsRelease", "true" },
            { "GITHUB_REPOSITORY_OWNER", "me" },
            { "GITHUB_REPOSITORY", "me/test" },
            { "GITHUB_ACTIONS", "true" },
            { "GITHUB_SERVER_URL", "https://github.com" },
            { "GITHUB_SHA", "1234" }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "org.opencontainers.image.authors", "me" },
            { "org.opencontainers.image.revision", "1234" },
            { "org.opencontainers.image.title", "me/test" },
            { "org.opencontainers.image.vendor", "me" },
            { "org.opencontainers.image.version", _tempVersion },
            { "org.opencontainers.image.documentation", "https://github.com/me/test/blob/1234/README.md" },
            { "org.opencontainers.image.url", "https://github.com/me/pkgs/container/test" },
            { "org.opencontainers.image.licenses", "https://github.com/me/test/blob/1234/LICENSE.md" },
            { "org.opencontainers.image.source", "https://github.com/me/test" },
            { "com.docker.extension.changelog", "https://github.com/me/test/blob/1234/CHANGELOG.md" },
            { "com.docker.extension.publisher-url", "https://github.com/me" }
        };
        await ContainerShouldContainMetadataAsync(metadata, _tempVersion, _cancellationToken);
    }

    [Test]
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "Performance it not that critical here.")]
    public async Task AppRunningInDocker_ShouldBeHealthy()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishRegularContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }
        };
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);
        var container = await StartAppInContainersAsync(_tempVersion, _cancellationToken);
        var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(container) };

        // Act
        var healthCheckResponse = await httpClient.GetAsync("healthz", _cancellationToken);
        var appResponse = await httpClient.GetAsync("/hello", _cancellationToken);
        var healthCheckToolResult = await container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", "http://localhost:8080/healthz"], _cancellationToken);

        // Assert
        await LogsShouldNotContainWarningsAsync(container, _cancellationToken);
        await HealthCheckShouldBeHealthyAsync(healthCheckResponse, _cancellationToken);
        await AppShouldRunAsync(appResponse, _cancellationToken);
        healthCheckToolResult.ExitCode.Should().Be(0);
    }

    private static async Task AppShouldRunAsync(HttpResponseMessage appResponse, CancellationToken cancellationToken)
    {
        appResponse.Should().Be200Ok();
        (await appResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Contain("World");
    }

    private static async Task HealthCheckShouldBeHealthyAsync(HttpResponseMessage healthCheckResponse, CancellationToken cancellationToken)
    {
        healthCheckResponse.Should().Be200Ok();
        (await healthCheckResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Be("Healthy");
    }

    private static async Task LogsShouldNotContainWarningsAsync(IContainer container, CancellationToken cancellationToken)
    {
        (string Stdout, string Stderr) logValues = await container.GetLogsAsync(ct: cancellationToken);
        Console.WriteLine($"Stderr:{Environment.NewLine}{logValues.Stderr}");
        Console.WriteLine($"Stdout:{Environment.NewLine}{logValues.Stdout}");
        logValues.Stdout.Should().NotContain("warn:");
    }

    private static CancellationToken CreateCancellationToken(TimeSpan timeout)
    {
        var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(timeout);
        var cancellationToken = timeoutCts.Token;

        return cancellationToken;
    }

    private static void CopyTestProject(DirectoryInfo directory)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectPath = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProjectViaNuGet");

        // Create all the directories
        foreach (var dirPath in Directory.GetDirectories(testProjectPath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(testProjectPath, directory.FullName, StringComparison.Ordinal));
        }

        // Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.GetFiles(testProjectPath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(testProjectPath, directory.FullName, StringComparison.Ordinal), true);
        }
    }

    private static async Task BuildNuGetPackageAsync(DirectoryInfo tempNugetDirectory, string nugetVersion, CancellationToken cancellationToken)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var projectFile = Path.Join(rootDirectory.FullName, "src", "mu88.Shared", "mu88.Shared.csproj");
        await WaitUntilDotnetToolSucceededAsync($"pack {projectFile} -p:Version={nugetVersion} -p:GenerateSBOM=false -o {tempNugetDirectory.FullName}",
            cancellationToken);
    }

    private static async Task AddNuGetPackageToTestProjectAsync(DirectoryInfo tempNugetDirectory,
                                                                DirectoryInfo tempTestProjectDirectory,
                                                                string nugetVersion,
                                                                CancellationToken cancellationToken) =>
        await WaitUntilDotnetToolSucceededAsync(
            $"add {GetTestProjectFilePath(tempTestProjectDirectory)} package mu88.Shared -v {nugetVersion} -s {tempNugetDirectory.FullName}",
            cancellationToken);

    private static async Task<IReadOnlyList<string>> BuildDockerImageOfAppAsync(DirectoryInfo tempTestProjectDirectory,
                                                                                Dictionary<string, string> buildParameters,
                                                                                CancellationToken cancellationToken)
    {
        buildParameters.Add("ContainerRegistry", string.Empty); // image shall not be pushed
        buildParameters.Add("ContainerRepository", "me/test");
        var arguments = string.Join(' ', buildParameters.Select(kvp => $"-p:{kvp.Key}=\"{kvp.Value}\""));

        return await WaitUntilDotnetToolSucceededAsync($"publish {GetTestProjectFilePath(tempTestProjectDirectory)} " +
                                                       "/t:PublishContainersForMultipleFamilies " +
                                                       $" {arguments}",
                   cancellationToken);
    }

    private static string GetTestProjectFilePath(DirectoryInfo tempTestProjectDirectory) =>
        Path.Join(tempTestProjectDirectory.FullName, "DummyAspNetCoreProjectViaNuGet.csproj");

    private static async Task<IContainer> StartAppInContainersAsync(string containerImageTag, CancellationToken cancellationToken)
    {
        Console.WriteLine("Building and starting network");
        var network = new NetworkBuilder().Build();
        await network.CreateAsync(cancellationToken);
        Console.WriteLine("Network started");

        Console.WriteLine("Building and starting app container");
        var container = BuildAppContainer(network, containerImageTag);
        await container.StartAsync(cancellationToken);
        Console.WriteLine("App container started");

        return container;
    }

    private static IContainer BuildAppContainer(INetwork network, string containerImageTag) =>
        new ContainerBuilder()
            .WithImage($"me/test:{containerImageTag}")
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                                  .UntilMessageIsLogged("Content root path: /app",
                                      strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))) // as it's a chiseled container, waiting for the port does not work
            .Build();

    private static Uri GetAppBaseAddress(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}");

    private static async Task<IReadOnlyList<string>> WaitUntilDotnetToolSucceededAsync(string arguments, CancellationToken cancellationToken) =>
        await WaitUntilToolSucceededAsync("dotnet", arguments, cancellationToken);

    private static async Task<IReadOnlyList<string>> WaitUntilToolSucceededAsync(string toolToStart, string arguments, CancellationToken cancellationToken)
    {
        var outputLines = new List<string>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = toolToStart, Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            }
        };
        process.Start();
        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            outputLines.Add(line);
            Console.WriteLine(line);
        }

        await process.WaitForExitAsync(cancellationToken);
        process.ExitCode.Should().Be(0);

        return outputLines;
    }

    private static async Task ContainerShouldContainMetadataAsync(Dictionary<string, string> metadata, string containerImageTag, CancellationToken cancellationToken)
    {
        var outputLines = await WaitUntilToolSucceededAsync("docker", $"inspect me/test:{containerImageTag}", cancellationToken);
        var dockerInspectResults = JsonNode.Parse(string.Join(string.Empty, outputLines));
        dockerInspectResults.Should().NotBeNull();
        var configLabels = dockerInspectResults.AsArray()[0]?["Config"]?["Labels"];
        configLabels.Should().NotBeNull();
        foreach (var (metadataKey, expectedValue) in metadata)
        {
            var currentValue = configLabels[metadataKey]?.ToString();
            currentValue.Should().Be(expectedValue, "because the container metadata key '{0}' should be set to '{1}'", metadataKey, expectedValue);
        }
    }

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me")]
    private static string GenerateVersion() => $"0.0.1-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
}