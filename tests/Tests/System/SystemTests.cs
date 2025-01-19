using System.Diagnostics;
using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FluentAssertions;

namespace Tests.System;

[Category("System")]
public class SystemTests
{
    [Test]
    public async Task AppRunningInDocker_ShouldBeHealthy()
    {
        // Arrange
        var cancellationToken = CreateCancellationToken(TimeSpan.FromMinutes(1));
        var tempDirectory = Directory.CreateTempSubdirectory("mu88_");
        try
        {
            var tempTestProjectDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "DummyAspNetCoreProjectViaNuGet"));
            var tempNuGetDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.FullName, "NuGet"));
            CopyTestProject(tempTestProjectDirectory);
            await BuildNuGetPackageAsync(tempNuGetDirectory, cancellationToken);
            await AddNuGetPackageToTestProjectAsync(tempNuGetDirectory, tempTestProjectDirectory, cancellationToken);
            await BuildDockerImageOfAppAsync(tempTestProjectDirectory, cancellationToken);
            var container = await StartAppInContainersAsync(cancellationToken);
            var httpClient = new HttpClient { BaseAddress = GetAppBaseAddress(container) };

            // Act
            var healthCheckResponse = await httpClient.GetAsync("healthz", cancellationToken);
            var appResponse = await httpClient.GetAsync("/hello", cancellationToken);
            var healthCheckToolResult = await container.ExecAsync(["dotnet", "/app/mu88.HealthCheck.dll", "http://localhost:8080/healthz"], cancellationToken);

            // Assert
            await LogsShouldNotContainWarningsAsync(container, cancellationToken);
            await HealthCheckShouldBeHealthyAsync(healthCheckResponse, cancellationToken);
            await AppShouldRunAsync(appResponse, cancellationToken);
            healthCheckToolResult.ExitCode.Should().Be(0);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    private static async Task AppShouldRunAsync(HttpResponseMessage appResponse, CancellationToken cancellationToken)
    {
        appResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await appResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Contain("World");
    }

    private static async Task HealthCheckShouldBeHealthyAsync(HttpResponseMessage healthCheckResponse, CancellationToken cancellationToken)
    {
        healthCheckResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await healthCheckResponse.Content.ReadAsStringAsync(cancellationToken)).Should().Be("Healthy");
    }

    private static async Task LogsShouldNotContainWarningsAsync(IContainer container, CancellationToken cancellationToken)
    {
        (string Stdout, string Stderr) logValues = await container.GetLogsAsync(ct: cancellationToken);
        Console.WriteLine($"Stderr:{Environment.NewLine}{logValues.Stderr}");
        Console.WriteLine($"Stdout:{Environment.NewLine}{logValues.Stdout}");
        logValues.Stdout.Should().NotContain("warn:");
    }

    private static CancellationToken CreateCancellationToken(TimeSpan timeout)
    {
        var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(timeout);
        var cancellationToken = timeoutCts.Token;

        return cancellationToken;
    }

    private static void CopyTestProject(DirectoryInfo directory)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var testProjectPath = Path.Join(rootDirectory.FullName, "DummyAspNetCoreProjectViaNuGet");

        // Create all the directories
        foreach (var dirPath in Directory.GetDirectories(testProjectPath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(testProjectPath, directory.FullName));

        // Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.GetFiles(testProjectPath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(testProjectPath, directory.FullName), true);
    }

    private static async Task BuildNuGetPackageAsync(DirectoryInfo tempNugetDirectory, CancellationToken cancellationToken)
    {
        var rootDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.Parent?.Parent ?? throw new NullReferenceException();
        var projectFile = Path.Join(rootDirectory.FullName, "src", "mu88.Shared", "mu88.Shared.csproj");
        await WaitUntilDotnetToolSucceededAsync($"pack {projectFile} -p:Version=99.99.99 -o {tempNugetDirectory.FullName}", cancellationToken);
    }

    private static async Task AddNuGetPackageToTestProjectAsync(DirectoryInfo tempNugetDirectory,
                                                                DirectoryInfo tempTestProjectDirectory,
                                                                CancellationToken cancellationToken)
    {
        var projectFile = Path.Join(tempTestProjectDirectory.FullName, "DummyAspNetCoreProjectViaNuGet.csproj");
        await WaitUntilDotnetToolSucceededAsync($"add {projectFile} package mu88.Shared -v 99.99.99 -s {tempNugetDirectory.FullName}", cancellationToken);
    }

    private static async Task BuildDockerImageOfAppAsync(DirectoryInfo tempTestProjectDirectory, CancellationToken cancellationToken)
    {
        var projectFile = Path.Join(tempTestProjectDirectory.FullName, "DummyAspNetCoreProjectViaNuGet.csproj");
        await WaitUntilDotnetToolSucceededAsync($"publish {projectFile} /t:MultiArchPublish -p:ContainerImageTags=local-system-test-chiseled",
            cancellationToken);
    }

    private static async Task<IContainer> StartAppInContainersAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Building and starting network");
        var network = new NetworkBuilder().Build();
        await network.CreateAsync(cancellationToken);
        Console.WriteLine("Network started");

        Console.WriteLine("Building and starting app container");
        var container = BuildAppContainer(network);
        await container.StartAsync(cancellationToken);
        Console.WriteLine("App container started");

        return container;
    }

    private static IContainer BuildAppContainer(INetwork network) =>
        new ContainerBuilder()
            .WithImage("mu88/mu88-shared-dummy-nuget:local-system-test-chiseled")
            .WithNetwork(network)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                                  .UntilMessageIsLogged("Content root path: /app",
                                      strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))) // as it's a chiseled container, waiting for the port does not work
            .Build();

    private static Uri GetAppBaseAddress(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(8080)}");

    private static async Task WaitUntilDotnetToolSucceededAsync(string arguments, CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet", Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            }
        };
        process.Start();
        while (!process.StandardOutput.EndOfStream) Console.WriteLine(await process.StandardOutput.ReadLineAsync(cancellationToken));

        await process.WaitForExitAsync(cancellationToken);
        process.ExitCode.Should().Be(0);
    }
}