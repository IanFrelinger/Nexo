using System.Text.Json.Serialization;

namespace Nexo.BackgroundAgents.Observations;

/// <summary>
/// Severity of an observation. Mirrors typical log levels but kept separate so
/// observation severity (which gates planner attention) can diverge from log
/// severity (which gates operator attention).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObservationSeverity
{
    Info,
    Warn,
    Error
}
