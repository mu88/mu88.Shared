using System.Diagnostics.CodeAnalysis;

namespace mu88.HealthCheck;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created", Justification = "Not a long-running app, i.e. disposing is not necessary")]
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don\'t ignore created IDisposable", Justification = "Not a long-running app, i.e. disposing is not necessary")]
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP008:Don\'t assign member with injected and created disposables", Justification = "Not a long-running app, i.e. disposing is not necessary")]
internal sealed class HealthChecker
{
    private readonly HttpClient _httpClient;

    public HealthChecker(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<int> CheckHealthAsync(string[] args)
    {
        _httpClient.DefaultRequestHeaders.ConnectionClose = true;

        if (args.Length == 1 && Uri.TryCreate(args[0], UriKind.RelativeOrAbsolute, out Uri? uri))
        {
            var response = await _httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                if (string.Equals(await response.Content.ReadAsStringAsync(), "Healthy", StringComparison.Ordinal))
                {
                    return 0;
                }
            }

            return 1;
        }

        throw new ArgumentException("A valid URI must be given as first argument", nameof(args));
    }
}