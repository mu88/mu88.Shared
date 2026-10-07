var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => Results.Content("<html><head><title>Dummy App</title></head><body></body></html>", "text/html"));

app.MapGet("/hello", () => "World");

await app.RunAsync();
