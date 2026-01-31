namespace mu88.Shared.Settings;

public class OpenTelemetryOptions
{
    public bool OpenTelemetryMetricsEnabled { get; set; } = true;

    public bool OpenTelemetryTracesEnabled { get; set; } = true;

    public bool OpenTelemetryLogsEnabled { get; set; } = true;
}