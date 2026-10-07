var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("external");

var app = builder.Build();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => Results.Content("<html><head><title>Dummy App</title></head><body></body></html>", "text/html"));
app.MapGet("/hello",
    (ILogger<Program> logger) =>
    {
        logger.LogInformation("Saying hello");
        return "World";
    });
app.MapGet("/call-external",
    async (IHttpClientFactory httpClientFactory, string url) =>
    {
        using var httpClient = httpClientFactory.CreateClient("external");
        var response = await httpClient.GetAsync(url);
        return response.IsSuccessStatusCode ? "OK" : "Fail";
    });

await app.RunAsync();
