namespace mu88.Shared.Settings;

public class Mu88SharedOptions
{
    public const string SectionName = "mu88Shared";

    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
}