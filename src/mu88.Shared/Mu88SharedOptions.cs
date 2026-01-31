namespace mu88.Shared;

public class Mu88SharedOptions
{
    public const string SectionName = "mu88Shared";

    public bool OpenTelemetryMetricsEnabled { get; set; } = true;

    public bool OpenTelemetryTracesEnabled { get; set; } = true;

    public bool OpenTelemetryLogsEnabled { get; set; } = true;
}