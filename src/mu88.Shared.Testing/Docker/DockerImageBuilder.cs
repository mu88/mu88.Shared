using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using CliWrap;
using CliWrap.Buffered;

namespace mu88.Shared.Testing.Docker;

/// <summary>
/// Builds an app's Docker image via Microsoft.NET.Build.Containers (through mu88.Shared's
/// PublishContainersForMultipleFamilies target) for use as the system-under-test in system tests.
/// </summary>
public static class DockerImageBuilder
{
    /// <summary>
    /// Generates a unique, Renovate-unrelated pre-release image tag for a single system test run, e.g.
    /// "0.0.0-system-test-1700000000".
    /// </summary>
    public static string GenerateContainerImageTag() =>
        $"0.0.0-system-test-{DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Publishes the app's Docker image locally (single-platform, linux/amd64) via
    /// "dotnet publish -t:PublishContainersForMultipleFamilies", so Testcontainers can start a container from it
    /// by its plain "repository:tag" name.
    /// </summary>
    /// <param name="projectFilePath">Absolute or working-directory-relative path to the app's .csproj.</param>
    /// <param name="imageTag">The tag to publish under, see <see cref="GenerateContainerImageTag"/>.</param>
    /// <param name="containerRepository">The image repository name, e.g. "podbridge-api".</param>
    /// <param name="workingDirectory">Working directory the "dotnet publish" command is executed in (typically the repo root).</param>
    /// <param name="cancellationToken">Token used to cancel the build.</param>
    /// <param name="additionalArguments">
    /// Extra "dotnet publish" arguments appended after the built-in ones, e.g. to override a default MSBuild
    /// property (the last occurrence of a given "-p:" property wins) or to pass app-specific build properties.
    /// </param>
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created", Justification = "False positive: the actual disposable (CommandTask<T>) is disposed via 'using'; 'result' is a plain non-disposable BufferedCommandResult.")]
    public static async Task BuildAsync(
        string projectFilePath,
        string imageTag,
        string containerRepository,
        string workingDirectory,
        CancellationToken cancellationToken,
        IReadOnlyCollection<string>? additionalArguments = null)
    {
        IEnumerable<string> arguments =
        [
            "publish", projectFilePath,
            "--os", "linux",
            "--arch", "amd64",
            "-t:PublishContainersForMultipleFamilies",
            $"-p:ReleaseVersion={imageTag}",
            "-p:IsRelease=false",
            "-p:DoNotApplyGitHubScope=true",
            $"-p:ContainerRepository={containerRepository}"
        ];
        if (additionalArguments != null)
        {
            arguments = arguments.Concat(additionalArguments);
        }

        using var commandTask = Cli.Wrap("dotnet")
            .WithArguments(arguments.ToArray())
            .WithWorkingDirectory(workingDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);
        var result = await commandTask;

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Container publish failed with exit code {result.ExitCode.ToString(CultureInfo.InvariantCulture)}. Output: {result.StandardOutput}. Error: {result.StandardError}");
        }
    }
}
