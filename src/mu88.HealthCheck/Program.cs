using System.Diagnostics.CodeAnalysis;

namespace mu88.HealthCheck;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient", Justification = "Not a long-running app, i.e. disposing is not necessary")]
[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var httpClient = new HttpClient();
        return await new HealthChecker(httpClient).CheckHealthAsync(args);
    }
}