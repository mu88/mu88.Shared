using FluentAssertions;
using mu88.Shared.Testing.Testcontainers;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class TestcontainerImagesTests
{
    private static readonly string[] PostgresStageOnly = ["FROM postgres:18-alpine AS postgres"];
    private static readonly string[] PostgresStageOnlyNewerVersion = ["FROM postgres:19-alpine AS postgres"];
    private static readonly string[] PostgresStageWithUppercaseAlias = ["FROM postgres:18-alpine AS Postgres"];

    private string _dockerfilePath = null!;

    [SetUp]
    public void SetUp() => _dockerfilePath = Path.Combine(Path.GetTempPath(), $"Dockerfile-{Guid.NewGuid():N}");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_dockerfilePath))
        {
            File.Delete(_dockerfilePath);
        }
    }

    [Test]
    public void GetImageFromDockerfile_ShouldReturnImage_WhenStageExists()
    {
        // Arrange
        var lines = new[]
        {
            "FROM postgres:18-alpine AS postgres",
            "FROM mcr.microsoft.com/playwright:v1.63.0-noble AS playwright"
        };
        File.WriteAllLines(_dockerfilePath, lines);

        // Act
        var image = TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "postgres");

        // Assert
        image.Should().Be("postgres:18-alpine");
    }

    [Test]
    public void GetImageFromDockerfile_ShouldIgnoreComments_AndBlankLines()
    {
        // Arrange
        var lines = new[]
        {
            "# this is a comment, not a FROM line",
            string.Empty,
            "FROM postgres:18-alpine AS postgres"
        };
        File.WriteAllLines(_dockerfilePath, lines);

        // Act
        var image = TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "postgres");

        // Assert
        image.Should().Be("postgres:18-alpine");
    }

    [Test]
    public void GetImageFromDockerfile_ShouldBeCaseInsensitive_RegardingStageName()
    {
        // Arrange
        File.WriteAllLines(_dockerfilePath, PostgresStageWithUppercaseAlias);

        // Act
        var image = TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "postgres");

        // Assert
        image.Should().Be("postgres:18-alpine");
    }

    [Test]
    public void GetImageFromDockerfile_ShouldThrow_WhenStageDoesNotExist()
    {
        // Arrange
        File.WriteAllLines(_dockerfilePath, PostgresStageOnly);

        // Act
        var act = () => TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "wiremock");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*wiremock*");
    }

    [Test]
    public void GetImageFromDockerfile_ShouldThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}");

        // Act
        var act = () => TestcontainerImages.GetImageFromDockerfile(nonExistingPath, "postgres");

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void GetImageFromDockerfile_ShouldCacheParsedDockerfile_WhenCalledTwiceForSameFile()
    {
        // Arrange
        File.WriteAllLines(_dockerfilePath, PostgresStageOnly);

        // Act
        var firstResult = TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "postgres");
        File.WriteAllLines(_dockerfilePath, PostgresStageOnlyNewerVersion);
        var secondResult = TestcontainerImages.GetImageFromDockerfile(_dockerfilePath, "postgres");

        // Assert
        firstResult.Should().Be("postgres:18-alpine");
        secondResult.Should().Be("postgres:18-alpine", "the parsed Dockerfile is cached per absolute file path");
    }
}
