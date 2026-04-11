namespace mu88.Shared.Settings;

internal sealed class OpenTelemetryOptions
{
    public bool MetricsEnabled { get; set; } = true;

    public bool LogsEnabled { get; set; } = true;

    public bool TracesEnabled { get; set; } = true;
}
