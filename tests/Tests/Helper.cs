using CliWrap;
using CliWrap.Buffered;
using FluentAssertions;

namespace Tests;

public static class Helper
{
    public static async Task<string> WaitUntilToolFinishedAsync(string toolToStart, IEnumerable<string> arguments, bool toolSucceeded, CancellationToken cancellationToken)
    {
        var buildResult = await Cli.Wrap(toolToStart)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);
        buildResult.IsSuccess.Should().Be(toolSucceeded);
        Console.WriteLine(buildResult.StandardOutput);

        return buildResult.StandardOutput.TrimEnd(Environment.NewLine.ToCharArray());
    }
}
