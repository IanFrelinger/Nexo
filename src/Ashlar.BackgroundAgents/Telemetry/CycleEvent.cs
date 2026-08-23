using System.Text;
using System.Text.Json;

namespace Ashlar.BackgroundAgents.Telemetry;

/// <summary>
/// One row of <c>cycles.jsonl</c>. Field names are intentionally short and
/// snake_case-ish lower for compact lines.
/// </summary>
public sealed record CycleEvent(
    DateTimeOffset ts,
    string agent,
    int cycle,
    string role,
    long duration_ms,
    int? iterations,
    int tools_executed,
    int tools_denied,
    string? model = null,
    string? provider = null,
    string? rationale = null,
    string? stopped_reason = null,
    bool success = true,
    string? error = null);
