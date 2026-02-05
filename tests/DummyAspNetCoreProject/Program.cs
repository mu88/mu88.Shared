var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/healthz");
app.MapGet("/hello", (ILogger<Program> logger) =>
{
    logger.LogInformation("Saying hello");
    return "World";
});

await app.RunAsync();