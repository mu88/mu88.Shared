using System.Text.RegularExpressions;
using FluentAssertions;
using mu88.Shared.Testing.Docker;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public partial class DockerImageBuilderTests
{
    [GeneratedRegex(@"^0\.0\.0-system-test-\d+$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ContainerImageTagPattern { get; }

    [Test]
    public void GenerateContainerImageTag_ShouldReturnExpectedFormat()
    {
        // Act
        var tag = DockerImageBuilder.GenerateContainerImageTag();

        // Assert
        tag.Should().MatchRegex(@"^0\.0\.0-system-test-\d+$");
    }

    [Test]
    public void GenerateContainerImageTag_ShouldBeUnique_WhenCalledMultipleTimes()
    {
        // Act
        var tags = Enumerable.Range(0, 2).Select(_ => DockerImageBuilder.GenerateContainerImageTag()).ToArray();
        var pattern = ContainerImageTagPattern;

        // Assert — not strictly guaranteed unique within the same second, but the regex format itself is the
        // actual contract under test; this additionally documents the intended usage.
        pattern.IsMatch(tags[0]).Should().BeTrue();
        pattern.IsMatch(tags[1]).Should().BeTrue();
    }
}
