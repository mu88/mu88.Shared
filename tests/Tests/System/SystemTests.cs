using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;
using NUnit.Framework.Interfaces;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private DirectoryInfo _tempDirectory;
    private DockerClient _dockerClient;
    private DirectoryInfo _tempTestProjectDirectory;
    private DirectoryInfo _tempNuGetDirectory;
    private string _tempVersion;
    private IContainer? _container;

    [SetUp]
    public void Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        _cancellationToken = _cancellationTokenSource.Token;
        _tempDirectory = Directory.CreateTempSubdirectory("mu88_Shared_SystemTests_");
        _dockerClient = new DockerClientConfiguration().CreateClient();
        _tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
        _tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(_tempDirectory.FullName, "NuGet"));
        _tempVersion = GenerateVersion();
    }

    [TearDown]
    public async Task Teardown()
    {
        _tempDirectory.Delete(true);

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
        {
            return; // no need to clean up on GitHub Actions runners
        }

        // If the test passed, clean up the container and image. Otherwise, keep them for investigation.
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed)
        {
            var dockerImageIds = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
                                 .Where(image => image.RepoTags.Any(tag => tag.Contains(_tempVersion, StringComparison.Ordinal)))
                                 .Select(image => image.ID)
                                 .Distinct(StringComparer.Ordinal);

            foreach (var dockerImageId in dockerImageIds)
            {
                var runningContainerIds = (await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters(), _cancellationToken))
                                          .Where(container => string.Equals(container.ImageID, dockerImageId, StringComparison.Ordinal))
                                          .Select(container => container.ID)
                                          .Distinct(StringComparer.Ordinal);
                foreach (var runningContainerId in runningContainerIds)
                {
                    await _dockerClient.Containers.StopContainerAsync(runningContainerId, new ContainerStopParameters(), _cancellationToken);
                    await _dockerClient.Containers.RemoveContainerAsync(runningContainerId, new ContainerRemoveParameters { Force = true }, _cancellationToken);
                }

                await _dockerClient.Images.DeleteImageAsync(dockerImageId, new ImageDeleteParameters { Force = true }, _cancellationToken);
            }
        }

        _dockerClient.Dispose();
        _cancellationTokenSource.Dispose();
    }

    [Test]
    public async Task PublishContainer_ShouldEmitCustomMsBuildPropertyWithComputedImageName()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" },
            { "PublishRegularContainer", "true" },
            { "ReleaseVersion", _tempVersion },
            { "IsRelease", "true" },
            { "GITHUB_REPOSITORY_OWNER", "mu88" },
            { "GITHUB_REPOSITORY", "mu88/mu88.Shared" },
            { "GITHUB_ACTIONS", "true" },
            { "GITHUB_SERVER_URL", "https://github.com" },
            { "GITHUB_SHA", "1234" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsyncWithoutPresetContainerRepository(_tempTestProjectDirectory,
                              buildParameters,
                              _cancellationToken,
                              "-getProperty:ComputedFullyQualifiedImageName");

        // Assert
        outputLines.Should().NotBeNull();
        outputLines.Should().HaveCount(1).And.Subject.Single().Should().Be("mu88/mu88-shared");
    }

    [Test]
    public async Task PublishContainer_ShouldEmitCustomMsBuildItemGroupWithGeneratedImages()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" },
            { "PublishRegularContainer", "true" },
            { "ReleaseVersion", _tempVersion },
            { "IsRelease", "true" },
            { "GITHUB_REPOSITORY_OWNER", "mu88" },
            { "GITHUB_REPOSITORY", "mu88/mu88.Shared" },
            { "GITHUB_ACTIONS", "true" },
            { "GITHUB_SERVER_URL", "https://github.com" },
            { "GITHUB_SHA", "1234" }
        };

        // Act
        var outputLines = await BuildDockerImageOfAppAsyncWithoutPresetContainerRepository(_tempTestProjectDirectory,
                              buildParameters,
                              _cancellationToken,
                              "-getItem:GeneratedImages");

        // Assert
        var dockerImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken);
        dockerImages.Should()
                    .Contain(image => image.RepoTags.Contains($"mu88/mu88-shared:{_tempVersion}") &&
                                      image.RepoTags.Contains("mu88/mu88-shared:latest"))
                    .And.Contain(image => image.RepoTags.Contains($"mu88/mu88-shared:{_tempVersion}-chiseled") &&
                                          image.RepoTags.Contains("mu88/mu88-shared:latest-chiseled"));
        outputLines.Should().NotBeEmpty();
        var json = new StringBuilder().AppendJoin(string.Empty, outputLines).ToString();
        var msBuildOutput = JsonSerializer.Deserialize<MsBuildOutput>(json);
        msBuildOutput?.Items.GeneratedImages.Should()
                     .NotBeNullOrEmpty()
                     .And.HaveCount(4)
                     .And.AllSatisfy(image => image.Identity.Should().Be("GeneratedImage"));
        msBuildOutput!.Items.GeneratedImages
                      .Select(image => image.ImageTag)
                      .Should()
                      .BeEquivalentTo(_tempVersion, "latest", $"{_tempVersion}-chiseled", "latest-chiseled");
        msBuildOutput.Items.GeneratedImages
                     .Select(image => image.FullyQualifiedImageWithTag)
                     .Should()
                     .BeEquivalentTo($"mu88/mu88-shared:{_tempVersion}",
                         "mu88/mu88-shared:latest",
                         $"mu88/mu88-shared:{_tempVersion}-chiseled",
                         "mu88/mu88-shared:latest-chiseled");
    }

    [Test]
    public async Task PublishContainer_ShouldEmitCustomMsBuildItemGroupWithGeneratedContainersProvidedByTheSDK()
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
        var outputLines = await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken, "-getItem:GeneratedContainers");

        // Assert
        outputLines.Should().NotBeEmpty();
        var json = new StringBuilder().AppendJoin(string.Empty, outputLines).ToString();
        var msBuildOutput = JsonSerializer.Deserialize<MsBuildOutput>(json);
        msBuildOutput?.Items.GeneratedContainers.Should()
                     .NotBeNullOrEmpty()
                     .And.HaveCount(4)
                     .And.AllSatisfy(image =>
                     {
                         image.Identity.Should().Be("GeneratedContainer");
                         image.ManifestDigest.Should().NotBeNullOrWhiteSpace();
                     });
    }

    [Test]
    public async Task PublishRegularContainer_ShouldUseCustomContainerBaseImageVersion()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishRegularContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }, { "CustomContainerBaseImageVersion", "9.0.11" }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var dockerImage = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
            .SingleOrDefault(image => image.RepoTags.Contains($"me/test:{_tempVersion}", StringComparer.Ordinal) &&
                                      image.RepoTags.Contains("me/test:latest", StringComparer.Ordinal));
        dockerImage.Should().NotBeNull();
        dockerImage.Labels.Should().ContainKey("org.opencontainers.image.base.name").WhoseValue.Should().Be("mcr.microsoft.com/dotnet/aspnet:9.0.11");
    }

    [Test]
    public async Task PublishChiseledContainer_ShouldUseCustomContainerBaseImageVersion()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }, { "CustomContainerBaseImageVersion", "9.0.11" }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var dockerImage = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
            .SingleOrDefault(image => image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled", StringComparer.Ordinal) &&
                                      image.RepoTags.Contains("me/test:latest-chiseled", StringComparer.Ordinal));
        dockerImage.Should().NotBeNull();
        dockerImage.Labels.Should().ContainKey("org.opencontainers.image.base.name").WhoseValue.Should().Be("mcr.microsoft.com/dotnet/aspnet:9.0.11-noble-chiseled");
    }

    [Test]
    public async Task PublishChiseledContainerWithExtra_ShouldUseCustomContainerBaseImageVersion()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" },
            { "InvariantGlobalization", "false" },
            { "ReleaseVersion", _tempVersion },
            { "IsRelease", "true" },
            { "CustomContainerBaseImageVersion", "9.0.11" }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var dockerImage = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
            .SingleOrDefault(image => image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled", StringComparer.Ordinal) &&
                                      image.RepoTags.Contains("me/test:latest-chiseled", StringComparer.Ordinal));
        dockerImage.Should().NotBeNull();
        dockerImage.Labels.Should()
                   .ContainKey("org.opencontainers.image.base.name")
                   .WhoseValue.Should()
                   .Be("mcr.microsoft.com/dotnet/aspnet:9.0.11-noble-chiseled-extra");
    }

    [Test]
    public async Task PublishRegularContainer_ShouldNotOverrideInitialContainerImageTags()
    {
        // Arrange
        _tempVersion = "1.0.0";
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishRegularContainer", "true" }, { "ReleaseVersion", "SomethingElse" }, { "IsRelease", "true" }, { "ContainerImageTags", _tempVersion }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken)).Should()
                                                                                                    .Contain(image => image.RepoTags.Contains($"me/test:{_tempVersion}"));
    }

    [Test]
    public async Task PublishChiseledContainer_ShouldNotOverrideInitialContainerImageTags()
    {
        // Arrange
        _tempVersion = "1.0.0";
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "ReleaseVersion", "SomethingElse" }, { "IsRelease", "true" }, { "ContainerImageTags", _tempVersion }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken)).Should()
                                                                                                    .Contain(image => image.RepoTags.Contains($"me/test:{_tempVersion}"));
    }

    [Test]
    public async Task PublishChiseledContainer_ShouldNotOverrideInitialContainerFamily()
    {
        // Arrange
        CopyTestProject(_tempTestProjectDirectory);
        await BuildNuGetPackageAsync(_tempNuGetDirectory, _tempVersion, _cancellationToken);
        await AddNuGetPackageToTestProjectAsync(_tempNuGetDirectory, _tempTestProjectDirectory, _tempVersion, _cancellationToken);
        Dictionary<string, string> buildParameters = new(StringComparer.Ordinal)
        {
            { "PublishChiseledContainer", "true" }, { "ReleaseVersion", _tempVersion }, { "IsRelease", "true" }, { "ContainerFamily", "alpine" }
        };

        // Act
        await BuildDockerImageOfAppAsync(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var dockerImage = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
            .SingleOrDefault(image => image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled", StringComparer.Ordinal) &&
                                      image.RepoTags.Contains("me/test:latest-chiseled", StringComparer.Ordinal));
        dockerImage.Should().NotBeNull();
        dockerImage.Labels.Should().ContainKey("org.opencontainers.image.base.name").WhoseValue.Should().Match("mcr.microsoft.com/dotnet/aspnet:*-alpine");
    }

    [Test]
    public async Task PublishRegularContainer()
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
                                                                                                    .Contain(image =>
                                                                                                        image.RepoTags.Contains($"me/test:{_tempVersion}")
                                                                                                        && image.RepoTags.Contains("me/test:latest"));
    }

    [Test]
    public async Task PublishChiseledContainer()
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
                                                                                                    .Contain(image =>
                                                                                                        image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled")
                                                                                                        && image.RepoTags.Contains("me/test:latest-chiseled"));
    }

    [Test]
    public async Task PublishChiseledContainerWithExtra()
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
        var dockerImage = (await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken))
            .SingleOrDefault(image => image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled", StringComparer.Ordinal) &&
                                      image.RepoTags.Contains("me/test:latest-chiseled", StringComparer.Ordinal));
        dockerImage.Should().NotBeNull();
        dockerImage.Labels.Should().ContainKey("org.opencontainers.image.base.name").WhoseValue.Should().Match("mcr.microsoft.com/dotnet/aspnet:*-noble-chiseled-extra");
    }

    [Test]
    public async Task PublishRegularAndChiseledContainer()
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
        var dockerImages = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters(), _cancellationToken);
        dockerImages.Should()
                    .Contain(image => image.RepoTags.Contains($"me/test:{_tempVersion}") &&
                                      image.RepoTags.Contains("me/test:latest"))
                    .And.Contain(image => image.RepoTags.Contains($"me/test:{_tempVersion}-chiseled") &&
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
            { "ContainerDescription", "This is an awesome project" },
            { "IsRelease", "true" },
            { "PublishRegularContainer", "true" },
            { "ReleaseVersion", _tempVersion },
            { "GITHUB_REPOSITORY_OWNER", "mu88" },
            { "GITHUB_REPOSITORY", "mu88/mu88.Shared" },
            { "GITHUB_ACTIONS", "true" },
            { "GITHUB_SERVER_URL", "https://github.com" },
            { "GITHUB_SHA", "1234" }
        };

        // Act
        await BuildDockerImageOfAppAsyncWithoutPresetContainerRepository(_tempTestProjectDirectory, buildParameters, _cancellationToken);

        // Assert
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "org.opencontainers.artifact.description", "This is an awesome project" },
            { "org.opencontainers.image.authors", "mu88" },
            { "org.opencontainers.image.description", "This is an awesome project" },
            { "org.opencontainers.image.revision", "1234" },
            { "org.opencontainers.image.title", "mu88/mu88.Shared" },
            { "org.opencontainers.image.vendor", "mu88" },
            { "org.opencontainers.image.version", _tempVersion },
            { "org.opencontainers.image.documentation", "https://github.com/mu88/mu88.Shared/blob/1234/README.md" },
            { "org.opencontainers.image.url", "https://github.com/mu88/pkgs/container/mu88.Shared" },
            { "org.opencontainers.image.licenses", "https://github.com/mu88/mu88.Shared/blob/1234/LICENSE.md" },
            { "org.opencontainers.image.source", "https://github.com/mu88/mu88.Shared" },
            { "com.docker.extension.changelog", "https://github.com/mu88/mu88.Shared/blob/1234/CHANGELOG.md" },
            { "com.docker.extension.publisher-url", "https://github.com/mu88" }
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
        _container = await StartAppInContainersAsync(_tempVersion, _cancellationToken);
        var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(_container) };

        // Act
        var healthCheckResponse = await httpClient.GetAsync("healthz", _cancellationToken);
        var appResponse = await httpClient.GetAsync("/hello", _cancellationToken);
        var healthCheckToolResult = await _container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", "http://127.0.0.1:8080/healthz"], _cancellationToken);

        // Assert
        await LogsShouldNotContainWarningsAsync(_container, _cancellationToken);
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
                                                                                CancellationToken cancellationToken,
                                                                                string? additionalArguments = null)
    {
        buildParameters.Add("ContainerRepository", "me/test"); // to avoid differences between running locally and in GitHub Actions (which sets this value automatically)
        return await BuildDockerImageOfAppAsyncWithoutPresetContainerRepository(tempTestProjectDirectory, buildParameters, cancellationToken, additionalArguments);
    }

    private static async Task<IReadOnlyList<string>> BuildDockerImageOfAppAsyncWithoutPresetContainerRepository(DirectoryInfo tempTestProjectDirectory,
                                                                                                                Dictionary<string, string> buildParameters,
                                                                                                                CancellationToken cancellationToken,
                                                                                                                string? additionalArguments = null)
    {
        buildParameters.Add("ContainerRegistry", string.Empty); // image shall not be pushed
        var arguments = string.Join(' ', buildParameters.Select(kvp => $"-p:{kvp.Key}=\"{kvp.Value}\""));
        return await WaitUntilDotnetToolSucceededAsync($"publish {GetTestProjectFilePath(tempTestProjectDirectory)} " +
                                                       "-t:PublishContainersForMultipleFamilies " +
                                                       $" {arguments} {additionalArguments}",
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
        new ContainerBuilder($"me/test:{containerImageTag}")
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                                  .UntilMessageIsLogged("Content root path: /app",
                                      strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))) // as it's a chiseled container, waiting for the port does not work
            .Build();

    private static Uri GetAppBaseAddress(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}");

    private static async Task<IReadOnlyList<string>> WaitUntilDotnetToolSucceededAsync(string arguments, CancellationToken cancellationToken) =>
        await WaitUntilToolSucceededAsync("dotnet", arguments, cancellationToken);

    private static async Task<IReadOnlyList<string>> WaitUntilToolSucceededAsync(string toolToStart, string arguments, CancellationToken cancellationToken) =>
        await Helper.WaitUntilToolFinishedAsync(toolToStart, arguments, true, cancellationToken);

    private static async Task ContainerShouldContainMetadataAsync(Dictionary<string, string> metadata, string containerImageTag, CancellationToken cancellationToken)
    {
        var outputLines = await WaitUntilToolSucceededAsync("docker", $"inspect mu88/mu88-shared:{containerImageTag}", cancellationToken);
        var dockerInspectResults = JsonNode.Parse(new StringBuilder().AppendJoin(string.Empty, outputLines).ToString());
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