using FluentAssertions;
using mu88.Shared.Testing.Docker;

namespace Tests.Integration;

[TestFixture]
[Category("Integration")]
public class DockerImageBuilderTests
{
    [Test]
    public async Task BuildAsync_ShouldDisableNodeReuseInChildBuild()
    {
        // Arrange
        var directory = Directory.CreateTempSubdirectory("mu88_Shared_WorkerLifetime_");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var projectPath = Path.Combine(directory.FullName, "Parent.proj");
            const string parentProject =
                """
                <Project>
                  <Target Name="Publish" />
                  <Target Name="PublishContainersForMultipleFamilies">
                    <MSBuild Projects="Worker.proj" Targets="Build" BuildInParallel="true" />
                  </Target>
                </Project>
                """;
            const string workerProject =
                """
                <Project>
                  <Target Name="Build">
                    <Error Condition="'$(MSBUILDDISABLENODEREUSE)' != '1'" Text="MSBuild worker reuse must be disabled for the child build." />
                    <WriteLinesToFile File="worker-completed.txt" Lines="completed" Overwrite="true" />
                  </Target>
                </Project>
                """;
            await File.WriteAllTextAsync(projectPath, parentProject, cancellation.Token);
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "Worker.proj"), workerProject, cancellation.Token);

            // Act
            await DockerImageBuilder.BuildAsync(projectPath, "0.0.0-test", "test", directory.FullName, cancellation.Token, ["-m:2"]);

            // Assert
            var result = await File.ReadAllTextAsync(Path.Combine(directory.FullName, "worker-completed.txt"), cancellation.Token);
            result.Trim().Should().Be("completed");
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
