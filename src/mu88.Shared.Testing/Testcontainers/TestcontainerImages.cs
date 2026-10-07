// Adapted from testcontainers-dotnet's internal test helper (MIT License):
// https://github.com/testcontainers/testcontainers-dotnet/blob/develop/tests/Testcontainers.Commons/TestSession.cs
// The original type is `internal`/not packaged; the maintainer declined to publish it separately (see
// https://github.com/testcontainers/testcontainers-dotnet/discussions/1782), so it is vendored here instead.
using System.Collections.Concurrent;

namespace mu88.Shared.Testing.Testcontainers;

/// <summary>
/// Resolves pinned Testcontainers image references from a (never-built) Dockerfile, so that Renovate's built-in
/// "dockerfile" manager can keep the referenced image versions up to date without any custom configuration.
/// </summary>
public static class TestcontainerImages
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Stages = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the image reference (e.g. "postgres:18-alpine") for the given stage of a Dockerfile.
    /// </summary>
    /// <param name="relativeFilePath">Path to the Dockerfile, relative to the current working directory.</param>
    /// <param name="stage">The stage name used in the Dockerfile's "FROM image:tag AS stage" line.</param>
    public static string GetImageFromDockerfile(string relativeFilePath, string stage)
    {
        var absoluteFilePath = Path.GetFullPath(relativeFilePath);

        var stages = Stages.GetOrAdd(absoluteFilePath, filePath => new DockerfileParser(filePath).Parse());

        if (stages.TryGetValue(stage, out var image))
        {
            return image;
        }

        throw new InvalidOperationException($"Stage '{stage}' not found in Dockerfile '{absoluteFilePath}'.");
    }
}
