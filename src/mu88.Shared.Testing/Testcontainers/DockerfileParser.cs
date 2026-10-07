// Adapted from testcontainers-dotnet's internal test helper (MIT License):
// https://github.com/testcontainers/testcontainers-dotnet/blob/develop/tests/Testcontainers.Commons/DockerfileParser.cs
// The original type is `internal` and not shipped as part of any NuGet package (see
// https://github.com/testcontainers/testcontainers-dotnet/discussions/1782), so it is vendored here instead.
using System.Text.RegularExpressions;

namespace mu88.Shared.Testing.Testcontainers;

internal sealed partial class DockerfileParser
{
    private readonly string _filePath;

    public DockerfileParser(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);

        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Dockerfile '{_filePath}' not found.");
        }
    }

    [GeneratedRegex("^FROM\\s+(?<arg>--\\S+\\s)*(?<image>\\S+).*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FromLinePattern { get; }

    public Dictionary<string, string> Parse()
    {
        const StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries;

        const string imageGroup = "image";

        var separator = new[] { " AS ", " As ", " aS ", " as " };

        var lines = File.ReadAllLines(_filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
            .ToArray();

        var fromMatches = lines
            .Select(line => FromLinePattern.Match(line))
            .Where(match => match.Success)
            .ToArray();

        var stages = fromMatches
            .Select(match => new
            {
                Stage = match.Value.Split(separator, options).Skip(1).FirstOrDefault(string.Empty),
                Image = match.Groups[imageGroup].Value
            })
            .ToDictionary(
                item => item.Stage,
                item => item.Image,
                StringComparer.OrdinalIgnoreCase);

        return stages;
    }
}
