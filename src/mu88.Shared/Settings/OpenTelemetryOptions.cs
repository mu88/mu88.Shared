namespace mu88.Shared.Settings;

public class OpenTelemetryOptions
{
    public bool MetricsEnabled { get; set; } = true;

    public bool LogsEnabled { get; set; } = true;

    public bool TracesEnabled { get; set; } = true;
}