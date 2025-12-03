using mu88.Shared.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

builder.ConfigureOpenTelemetry("test");

var app = builder.Build();

app.UseHttpsRedirection();
app.MapHealthChecks("/healthz");

app.MapGet("/hello", () => "World");

await app.RunAsync();