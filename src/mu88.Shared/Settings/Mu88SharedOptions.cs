namespace mu88.Shared.Settings;

internal sealed class Mu88SharedOptions
{
    public const string SectionName = "mu88Shared";

    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
}
