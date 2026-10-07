namespace Tests;

/// <summary>
/// Shared helper to build a local mu88.Shared NuGet package and consume it from a throwaway copy of
/// DummyAspNetCoreProjectViaNuGet, mirroring exactly how a real external consumer (e.g. RaspiFanController)
/// references mu88.Shared - i.e. via an actual NuGet package, not a ProjectReference. Used by both
/// Tests.System.SystemTests (testing mu88.Shared's own MSBuild targets) and
/// Tests.Integration.SystemTestsBaseDockerTests (dogfooding mu88.Shared.Testing's SystemTestsBase).
/// </summary>
internal static class NuGetTestProjectHelper
{
    public static string GetTestProjectFilePath(DirectoryInfo tempTestProjectDirectory) =>
        Path.Join(tempTestProjectDirectory.FullName, "DummyAspNetCoreProjectViaNuGet.csproj");

    /// <summary>
    /// Copies the static DummyAspNetCoreProjectViaNuGet template project into <paramref name="directory"/>, so the
    /// NuGet package reference added by <see cref="AddNuGetPackageToTestProjectAsync"/> doesn't leave any trace in
    /// the repository itself.
    /// </summary>
    public static void CopyTestProject(DirectoryInfo directory)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectPath = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProjectViaNuGet");

        // Create all the directories
        foreach (var dirPath in Directory.GetDirectories(testProjectPath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(testProjectPath, directory.FullName, StringComparison.Ordinal));
        }

        // Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.GetFiles(testProjectPath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(testProjectPath, directory.FullName, StringComparison.Ordinal), true);
        }
    }

    /// <summary>
    /// Packs mu88.Shared (the project under test) as a local NuGet package tagged <paramref name="nugetVersion"/>
    /// into <paramref name="tempNugetDirectory"/>, acting as a throwaway local feed.
    /// </summary>
    public static async Task BuildNuGetPackageAsync(DirectoryInfo tempNugetDirectory, string nugetVersion, CancellationToken cancellationToken)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var projectFile = Path.Join(rootDirectory.FullName, "src", "mu88.Shared", "mu88.Shared.csproj");
        IEnumerable<string> arguments = ["pack", projectFile, $"-p:Version={nugetVersion}", "-p:GenerateSBOM=false", "-o", tempNugetDirectory.FullName];
        await Helper.WaitUntilToolFinishedAsync("dotnet", arguments, true, cancellationToken);
    }

    /// <summary>
    /// Adds the local mu88.Shared NuGet package built by <see cref="BuildNuGetPackageAsync"/> to the copied test
    /// project, exactly as an external consumer would via "dotnet add package".
    /// </summary>
    public static async Task AddNuGetPackageToTestProjectAsync(
        DirectoryInfo tempNugetDirectory,
        DirectoryInfo tempTestProjectDirectory,
        string nugetVersion,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> addArguments =
            ["add", GetTestProjectFilePath(tempTestProjectDirectory), "package", "mu88.Shared", "-v", nugetVersion, "-s", tempNugetDirectory.FullName];
        await Helper.WaitUntilToolFinishedAsync("dotnet", addArguments, true, cancellationToken);
        IEnumerable<string> restoreArguments = ["restore", GetTestProjectFilePath(tempTestProjectDirectory)];
        await Helper.WaitUntilToolFinishedAsync("dotnet", restoreArguments, true, cancellationToken);
    }
}
