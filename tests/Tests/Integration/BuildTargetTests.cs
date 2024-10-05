using System.Diagnostics;
using FluentAssertions;

namespace Tests.Integration;

[TestFixture]
[Category("Integration")]
public class BuildTargetTests
{
    [Test]
    public void MultiArchPublish_ShouldPublishMultiArchImage()
    {
        // Arrange
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet", Arguments = $"publish {GetTestProjectFile()} /t:MultiArchPublish", RedirectStandardOutput = true, UseShellExecute = false
        };

        // Act
        var process = Process.Start(processStartInfo);
        var output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Assert
        Console.WriteLine(output);
        output.Should().Contain("Created and pushed manifest list registry.hub.docker.com/mu88/mu88-shared-dummy:dev");
    }

    private static string GetTestProjectFile() => Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent!.Parent!.Parent!.Parent!.FullName,
        "DummyAspNetCoreProject",
        "DummyAspNetCoreProject.csproj");
}