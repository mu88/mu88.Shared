using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class SharedTargetsTests
{
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "False positive")]
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "False positive")]
    public static object[] Cases_PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing()
    {
        return
        [
            new object[]
            {
                "At least one of PublishRegularContainer or PublishChiseledContainer must be 'true'",
                new Dictionary<string, string>(StringComparer.Ordinal) { { "PublishChiseledContainer", "false" }, { "PublishRegularContainer", "false" } }
            },
            new object[]
            {
                "PublishRegularContainer property must be 'true' or 'false'",
                new Dictionary<string, string>(StringComparer.Ordinal) { { "PublishChiseledContainer", "true" }, { "PublishRegularContainer", "bla" } }
            },
            new object[]
            {
                "PublishChiseledContainer property must be 'true' or 'false'",
                new Dictionary<string, string>(StringComparer.Ordinal) { { "PublishChiseledContainer", "bla" }, { "PublishRegularContainer", "true" } }
            },
            new object[]
            {
                "IsRelease property must be set ('true' or 'false')",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "PublishChiseledContainer", "true" }, { "IsRelease", string.Empty }, { "ReleaseVersion", "dev" }
                }
            },
            new object[]
            {
                "IsRelease property must be set ('true' or 'false')",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "PublishRegularContainer", "true" }, { "IsRelease", string.Empty }, { "ReleaseVersion", "dev" }
                }
            },
            new object[]
            {
                "ReleaseVersion property must be set (e.g., '1.2.3')",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "PublishChiseledContainer", "true" }, { "IsRelease", "true" }, { "ReleaseVersion", string.Empty }
                }
            },
            new object[]
            {
                "ReleaseVersion property must be set (e.g., '1.2.3')",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "PublishRegularContainer", "true" }, { "IsRelease", "true" }, { "ReleaseVersion", string.Empty }
                }
            }
        ];
    }

    [TestCaseSource(nameof(Cases_PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing))]
    public async Task PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing(string expectedErrorMessage,
                                                                                                        [SuppressMessage("Design",
                                                                                                            "MA0016:Prefer using collection abstraction instead of implementation",
                                                                                                            Justification = "Limitation of NUnit")]
                                                                                                        Dictionary<string, string> buildParameters)
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        buildParameters.Add("DryRun", "true");
        var arguments =
            $"publish {testProjectFile} /t:PublishContainersForMultipleFamilies /p:DoNotApplyGitHubScope=true {string.Join(' ', buildParameters.Select(kvp => $"-p:{kvp.Key}=\"{kvp.Value}\""))}";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, false, CancellationToken.None);

        // Assert
        outputLines.Should().ContainMatch($"*{expectedErrorMessage}*");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldPrecomputeContainerRepository_WhenNotSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments = $"msbuild {testProjectFile} /t:PrecomputeContainerRepository /p:DoNotApplyGitHubScope=true --getProperty:ComputedContainerRepository";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("dummyaspnetcoreproject");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldUseSpecifiedContainerRepository_WhenSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments =
            $"msbuild {testProjectFile} /t:PrecomputeContainerRepository /p:DoNotApplyGitHubScope=true /p:ContainerRepository=\"me/test\" --getProperty:ComputedContainerRepository";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("me/test");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldPrecomputeFullyQualifiedImageWithoutRegistry_WhenNotSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments = $"msbuild {testProjectFile} /t:PrecomputeFullyQualifiedImage /p:DoNotApplyGitHubScope=true --getProperty:ComputedFullyQualifiedImage";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("dummyaspnetcoreproject");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldPrecomputeFullyQualifiedImageWithRegistry_WhenNotSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments =
            $"msbuild {testProjectFile} /t:PrecomputeFullyQualifiedImage /p:DoNotApplyGitHubScope=true /p:ContainerRegistry=\"ghcr.io\" --getProperty:ComputedFullyQualifiedImage";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("ghcr.io/dummyaspnetcoreproject");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldUseSpecifiedContainerRepositoryWithoutRegistry_WhenSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments =
            $"msbuild {testProjectFile} /t:PrecomputeFullyQualifiedImage /p:DoNotApplyGitHubScope=true /p:ContainerRepository=\"me/test\" --getProperty:ComputedFullyQualifiedImage";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("me/test");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldUseSpecifiedContainerRepositoryWithRegistry_WhenSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        var arguments =
            $"msbuild {testProjectFile} /t:PrecomputeFullyQualifiedImage /p:DoNotApplyGitHubScope=true /p:ContainerRegistry=\"ghcr.io\" /p:ContainerRepository=\"me/test\" --getProperty:ComputedFullyQualifiedImage";

        // Act
        var outputLines = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        outputLines.Should().HaveCount(1).And.ContainMatch("ghcr.io/me/test");
    }
}