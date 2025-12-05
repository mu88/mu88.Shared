using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    [Test]
    public async Task PublishContainer_ShouldPublishRegularContainer()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);

            // Act
            var outputLines = await DryRunContainerPublishingAsync(tempTestProjectDirectory,
                                  $"-p:PublishRegularContainer=true -p:ReleaseVersion={nugetVersion}",
                                  cancellationToken);

            // Assert
            outputLines.Should().ContainMatch($"*Publishing regular container image with tags: {nugetVersion};latest");
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Test]
    public async Task PublishContainer_ShouldPublishChiseledContainer()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);

            // Act
            var outputLines = await DryRunContainerPublishingAsync(tempTestProjectDirectory,
                                  $"-p:PublishChiseledContainer=true -p:ReleaseVersion={nugetVersion}",
                                  cancellationToken);

            // Assert
            outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {nugetVersion}-chiseled;latest-chiseled");
            outputLines.Should().ContainMatch("*Using container base image: mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled");
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Test]
    public async Task PublishContainer_ShouldPublishChiseledContainerWithExtra()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);

            // Act
            var outputLines = await DryRunContainerPublishingAsync(tempTestProjectDirectory,
                                  $"-p:PublishChiseledContainer=true -p:InvariantGlobalization=false -p:ReleaseVersion={nugetVersion}",
                                  cancellationToken);

            // Assert
            outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {nugetVersion}-chiseled;latest-chiseled");
            outputLines.Should().ContainMatch("*Using container base image: mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled-extra");
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Test]
    public async Task PublishContainer_ShouldPublishRegularAndChiseledContainer()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);

            // Act
            var outputLines = await DryRunContainerPublishingAsync(tempTestProjectDirectory,
                                  $"-p:PublishRegularContainer=true -p:PublishChiseledContainer=true -p:ReleaseVersion={nugetVersion}",
                                  cancellationToken);

            // Assert
            outputLines.Should().ContainMatch($"*Publishing regular container image with tags: {nugetVersion};latest");
            outputLines.Should().ContainMatch($"*Publishing chiseled container image with tags: {nugetVersion}-chiseled;latest-chiseled");
            outputLines.Should().ContainMatch("*Using container base image: mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled");
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Test]
    public async Task PublishContainer_ShouldSetContainerMetadata()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var containerImageTag = GenerateContainerImageTag();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);

            // Act
            await BuildDockerImageOfAppAndMimicGitHubAsync(tempTestProjectDirectory, containerImageTag, cancellationToken);

            // Assert
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "org.opencontainers.image.authors", "me" },
                { "org.opencontainers.image.revision", "1234" },
                { "org.opencontainers.image.title", "me/test" },
                { "org.opencontainers.image.vendor", "me" },
                { "org.opencontainers.image.version", $"{containerImageTag}" },
                { "org.opencontainers.image.documentation", "https://github.com/me/test/blob/1234/README.md" },
                { "org.opencontainers.image.url", "https://github.com/me/pkgs/container/test" },
                { "org.opencontainers.image.licenses", "https://github.com/me/test/blob/1234/LICENSE.md" },
                { "org.opencontainers.image.source", "https://github.com/me/test" },
                { "com.docker.extension.changelog", "https://github.com/me/test/blob/1234/CHANGELOG.md" },
                { "com.docker.extension.publisher-url", "https://github.com/me" }
            };
            await ContainerShouldContainMetadataAsync(metadata, containerImageTag, cancellationToken);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Test]
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "Performance it not that critical here.")]
    public async Task AppRunningInDocker_ShouldBeHealthy()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        try
        {
            var nugetVersion = GenerateNuGetVersion();
            var containerImageTag = GenerateContainerImageTag();
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, nugetVersion, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, nugetVersion, cancellationToken);
            await BuildDockerImageOfAppAsync(tempTestProjectDirectory, string.Empty, containerImageTag, cancellationToken);
            var container = await StartAppInContainersAsync(containerImageTag, cancellationToken);
            var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(container) };

            // Act
            var healthCheckResponse = await httpClient.GetAsync("healthz", cancellationToken);
            var appResponse = await httpClient.GetAsync("/hello", cancellationToken);
            var healthCheckToolResult = await container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", "http://localhost:8080/healthz"], cancellationToken);

            // Assert
            await LogsShouldNotContainWarningsAsync(container, cancellationToken);
            await HealthCheckShouldBeHealthyAsync(healthCheckResponse, cancellationToken);
            await AppShouldRunAsync(appResponse, cancellationToken);
            healthCheckToolResult.ExitCode.Should().Be(0);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
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

    private static async Task<IReadOnlyList<string>> DryRunContainerPublishingAsync(DirectoryInfo tempTestProjectDirectory,
                                                                                    string additionalBuildParameters,
                                                                                    CancellationToken cancellationToken) =>
        await WaitUntilDotnetToolSucceededAsync($"msbuild {GetTestProjectFilePath(tempTestProjectDirectory)} " +
                                                "/t:PublishContainersForMultipleFamilies " +
                                                "-p:PublishRegularContainer=true " +
                                                "-p:ReleaseVersion=dev " +
                                                "-p:IsRelease=true " +
                                                "-p:DryRun=true " +
                                                $"{additionalBuildParameters}",
            cancellationToken);

    private static async Task AddNuGetPackageToTestProjectAsync(DirectoryInfo tempNugetDirectory,
                                                                DirectoryInfo tempTestProjectDirectory,
                                                                string nugetVersion,
                                                                CancellationToken cancellationToken) =>
        await WaitUntilDotnetToolSucceededAsync(
            $"add {GetTestProjectFilePath(tempTestProjectDirectory)} package mu88.Shared -v {nugetVersion} -s {tempNugetDirectory.FullName}",
            cancellationToken);

    private static async Task BuildDockerImageOfAppAsync(DirectoryInfo tempTestProjectDirectory,
                                                         string additionalBuildParameters,
                                                         string containerImageTag,
                                                         CancellationToken cancellationToken) =>
        await WaitUntilDotnetToolSucceededAsync($"publish {GetTestProjectFilePath(tempTestProjectDirectory)} " +
                                                "/t:PublishContainersForMultipleFamilies " +
                                                "-p:PublishRegularContainer=true " +
                                                $"-p:ReleaseVersion={containerImageTag} " +
                                                "-p:IsRelease=false " +
                                                "-p:ContainerRegistry=\"\" " + // image shall not be pushed
                                                "-p:ContainerRepository=\"me/test\" " +
                                                $"{additionalBuildParameters}",
            cancellationToken);

    private static string GetTestProjectFilePath(DirectoryInfo tempTestProjectDirectory) =>
        Path.Join(tempTestProjectDirectory.FullName, "DummyAspNetCoreProjectViaNuGet.csproj");

    private static async Task BuildDockerImageOfAppAndMimicGitHubAsync(DirectoryInfo tempTestProjectDirectory,
                                                                       string containerImageTag,
                                                                       CancellationToken cancellationToken)
    {
        var additionalBuildParameters = "-p:ContainerRegistry=\"\" " + // image shall not be pushed
                                        "-p:GITHUB_REPOSITORY_OWNER=me " +
                                        "-p:GITHUB_REPOSITORY=\"me/test\" " +
                                        "-p:GITHUB_ACTIONS=true " +
                                        "-p:GITHUB_SERVER_URL=\"https://github.com\" " +
                                        "-p:GITHUB_SHA=1234";
        await BuildDockerImageOfAppAsync(tempTestProjectDirectory,
            additionalBuildParameters,
            containerImageTag,
            cancellationToken);
    }

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
    private static string GenerateContainerImageTag() => $"system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

    [SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "Okay for me")]
    private static string GenerateNuGetVersion() => $"0.0.1-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
}