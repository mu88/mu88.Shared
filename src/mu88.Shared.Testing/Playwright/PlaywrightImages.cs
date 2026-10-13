using System.Text.Json;

namespace mu88.Shared.Testing.Playwright;

/// <summary>
/// Derives the matching Playwright server Docker image tag from the Playwright Node.js driver that
/// Microsoft.Playwright actually resolved for the calling test assembly - instead of relying on a
/// separately tracked version declaration (e.g. a Dockerfile parsed via
/// <see cref="Testcontainers.TestcontainerImages"/>) that could drift from what is actually loaded.
/// Microsoft.Playwright's own build/buildTransitive targets already copy its driver manifest
/// ("package.json") next to every consumer's test output - this reads that manifest directly, so the
/// resulting tag always matches the Microsoft.Playwright NuGet version actually resolved for THIS build,
/// regardless of where (or whether) that version is pinned.
/// </summary>
public static class PlaywrightImages
{
    private const string PackageJsonRelativePath = ".playwright/package/package.json";

    /// <summary>
    /// Returns the Playwright server image reference (e.g. "mcr.microsoft.com/playwright:v1.63.0-noble")
    /// matching the Microsoft.Playwright NuGet package actually resolved for the calling test assembly.
    /// </summary>
    /// <param name="distro">
    /// The Ubuntu base image flavor used by the official Playwright server image (e.g. "noble", "jammy").
    /// Defaults to "noble"; override for consumers pinned to an older Microsoft.Playwright version for
    /// which no "-noble" tag exists (available starting with Playwright v1.45.0) or that deliberately use
    /// a different distro.
    /// </param>
    public static string GetServerImageTag(string distro = "noble") =>
        BuildServerImageTag(Path.Combine(AppContext.BaseDirectory, PackageJsonRelativePath), distro);

    /// <summary>
    /// Builds the image tag from an already-resolved package.json path. Extracted from
    /// <see cref="GetServerImageTag"/> so the parsing/formatting logic can be unit-tested without depending
    /// on the test host's own <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    internal static string BuildServerImageTag(string packageJsonPath, string distro)
    {
        if (!File.Exists(packageJsonPath))
        {
            throw new FileNotFoundException(
                $"Playwright driver manifest not found at '{packageJsonPath}'. " +
                "Ensure Microsoft.Playwright is referenced (directly or transitively) by the test project.",
                packageJsonPath);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
        var version = document.RootElement.TryGetProperty("version", out var versionProperty) ? versionProperty.GetString() : null;

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException($"'version' field missing or empty in '{packageJsonPath}'.");
        }

        return $"mcr.microsoft.com/playwright:v{version}-{distro}";
    }
}
