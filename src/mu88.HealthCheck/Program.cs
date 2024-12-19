using System.Diagnostics.CodeAnalysis;

namespace mu88.HealthCheck;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "Not a long-running app, i.e. disposing is not necessary")]
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don\'t ignore created IDisposable", Justification = "Not a long-running app, i.e. disposing is not necessary")]
[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task<int> Main(string[] args) => await new HealthChecker(new HttpClient()).CheckHealthAsync(args);
}