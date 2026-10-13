using FluentAssertions;
using mu88.Shared.Testing.Playwright;

namespace Tests.Unit;

[TestFixture]
[Category("Unit")]
public class PlaywrightImagesTests
{
    private string _packageJsonPath = null!;

    [SetUp]
    public void SetUp() => _packageJsonPath = Path.Combine(Path.GetTempPath(), $"package-{Guid.NewGuid():N}.json");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_packageJsonPath))
        {
            File.Delete(_packageJsonPath);
        }
    }

    [Test]
    public void BuildServerImageTag_ShouldReturnMatchingImageTag_WhenPackageJsonExists()
    {
        // Arrange
        File.WriteAllText(_packageJsonPath, """{ "name": "playwright-core", "version": "1.63.0" }""");

        // Act
        var image = PlaywrightImages.BuildServerImageTag(_packageJsonPath, "noble");

        // Assert
        image.Should().Be("mcr.microsoft.com/playwright:v1.63.0-noble");
    }

    [Test]
    public void BuildServerImageTag_ShouldUseGivenDistro()
    {
        // Arrange
        File.WriteAllText(_packageJsonPath, """{ "version": "1.45.0" }""");

        // Act
        var image = PlaywrightImages.BuildServerImageTag(_packageJsonPath, "jammy");

        // Assert
        image.Should().Be("mcr.microsoft.com/playwright:v1.45.0-jammy");
    }

    [Test]
    public void BuildServerImageTag_ShouldThrow_WhenPackageJsonDoesNotExist()
    {
        // Act
        var act = () => PlaywrightImages.BuildServerImageTag(_packageJsonPath, "noble");

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void BuildServerImageTag_ShouldThrow_WhenVersionFieldIsMissing()
    {
        // Arrange
        File.WriteAllText(_packageJsonPath, """{ "name": "playwright-core" }""");

        // Act
        var act = () => PlaywrightImages.BuildServerImageTag(_packageJsonPath, "noble");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*version*");
    }

    [Test]
    public void GetServerImageTag_ShouldReturnRealResolvedVersion_WhenCalledFromTestHost()
    {
        // Act - exercises the real AppContext.BaseDirectory lookup against the Microsoft.Playwright driver
        // manifest that is actually copied into this test project's own output directory.
        var image = PlaywrightImages.GetServerImageTag();

        // Assert
        image.Should().MatchRegex(@"^mcr\.microsoft\.com/playwright:v\d+\.\d+\.\d+-noble$");
    }
}
