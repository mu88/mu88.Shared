using mu88.Shared.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.ConfigureOpenTelemetry("test");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapHealthChecks("/healthz");

app.MapGet("/hello", () => "World")
   .WithOpenApi();

await app.RunAsync();