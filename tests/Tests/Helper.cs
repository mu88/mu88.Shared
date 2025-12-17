using System.Diagnostics;
using FluentAssertions;

namespace Tests;

public static class Helper
{
    public static async Task<IReadOnlyList<string>> WaitUntilToolFinishedAsync(string toolToStart, string arguments, bool toolSucceeded, CancellationToken cancellationToken)
    {
        var outputLines = new List<string>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = toolToStart, Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            }
        };
        process.Start();
        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            outputLines.Add(line);
            Console.WriteLine(line);
        }

        await process.WaitForExitAsync(cancellationToken);
        (process.ExitCode == 0).Should().Be(toolSucceeded);

        return outputLines;
    }
}