using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class SharedTargetsTests
{
    [TestCaseSource(nameof(Cases_PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing))]
    public async Task PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing(
        string expectedErrorMessage,
        [SuppressMessage("Design",
            "MA0016:Prefer using collection abstraction instead of implementation",
            Justification = "Limitation of NUnit")]
        Dictionary<string, string> buildParameters)
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        buildParameters.Add("DryRun", "true");
        IEnumerable<string> arguments = ["publish", testProjectFile, "-t:PublishContainersForMultipleFamilies", "-p:DoNotApplyGitHubScope=true"];

        // Act
        var standardOutput = await Helper.WaitUntilToolFinishedAsync("dotnet",
            arguments.Concat(buildParameters.Select(kvp => $"-p:{kvp.Key}={kvp.Value}")),
            false,
            CancellationToken.None);

        // Assert
        standardOutput.Should().Match($"*{expectedErrorMessage}*");
    }

    [TestCase("-p:ContainerRegistry=ghcr.io", "ghcr.io/dummyaspnetcoreproject")]
    [TestCase("-p:ContainerRegistry=", "dummyaspnetcoreproject")]
    public async Task PublishContainersForMultipleFamilies_ShouldComputeFullyQualifiedImageName(string buildArguments, string expectedFullyQualifiedImageName)
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        IEnumerable<string> arguments =
        [
            "publish",
            testProjectFile,
            "-t:PublishContainersForMultipleFamilies",
            "-p:IsRelease=false",
            "-p:ReleaseVersion=dev",
            "-p:DoNotApplyGitHubScope=true",
            buildArguments,
            "-p:DryRun=true",
            "-getProperty:ComputedFullyQualifiedImageName"
        ];

        // Act
        var standardOutput = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);
        standardOutput.Should().NotBeNull();
        standardOutput.Should().Be(expectedFullyQualifiedImageName);
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldPrecomputeContainerRepository_WhenNotSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        IEnumerable<string> arguments =
        [
            "msbuild", testProjectFile, "-t:PrecomputeContainerRepository", "-p:DoNotApplyGitHubScope=true", "-getProperty:ComputedContainerRepository"
        ];

        // Act
        var standardOutput = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        standardOutput.Should().BeEquivalentTo("dummyaspnetcoreproject");
    }

    [Test]
    public async Task PublishContainerForMultipleFamilies_ShouldUseSpecifiedContainerRepository_WhenSet()
    {
        // Arrange
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectFile = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProject", "DummyAspNetCoreProject.csproj");
        IEnumerable<string> arguments =
        [
            "msbuild",
            testProjectFile,
            "-t:PrecomputeContainerRepository",
            "-p:DoNotApplyGitHubScope=true",
            "-p:ContainerRepository=\"me/test\"",
            "-getProperty:ComputedContainerRepository"
        ];

        // Act
        var standardOutput = await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, CancellationToken.None);

        // Assert
        standardOutput.Should().Match("me/test");
    }

    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "False positive")]
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "False positive")]
    private static object[] Cases_PublishContainersForMultipleFamilies_ShouldFail_WhenPropertiesAreInvalidOrMissing()
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
                new Dictionary<string, string>(StringComparer.Ordinal) { { "PublishRegularContainer", "true" }, { "IsRelease", string.Empty }, { "ReleaseVersion", "dev" } }
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
}
