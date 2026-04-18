using System.Text.Json.Serialization;

namespace Nexo.BackgroundAgents.Observations;

/// <summary>
/// One row of <c>observations.jsonl</c>: a fact discovered by some agent (or by
/// the planner's own tool calls) that other agents — especially the planner —
/// can react to in their next cycle.
///
/// <para>This is the second persistence surface introduced for the multi-agent
/// loop, complementary to <c>cycles.jsonl</c>. Where <c>cycles.jsonl</c> answers
/// "did the daemon do work?", <c>observations.jsonl</c> answers "what does the
/// daemon collectively know right now?".</para>
///
/// <para>The type is named <c>RuntimeObservation</c> rather than
/// <c>Observation</c> because the existing <c>Nexo.BackgroundAgents.Observation</c>
/// namespace (the OS-level event/pattern pipeline) shadows the unqualified name.</para>
///
/// <para>Field names are short and snake_case to keep JSONL lines compact when
/// hundreds of observations land in a single day.</para>
/// </summary>
public sealed record RuntimeObservation(
    DateTimeOffset ts,
    string source,
    ObservationKind kind,
    string summary,
    [property: JsonPropertyName("severity")] ObservationSeverity severity = ObservationSeverity.Info,
    [property: JsonPropertyName("facts")] IReadOnlyDictionary<string, string>? facts = null,
    [property: JsonPropertyName("agent_cycle")] int? agentCycle = null);

/// <summary>
/// Categorises observations so consumers can subscribe to a slice without
/// parsing arbitrary summary strings.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObservationKind
{
    /// <summary>Result of running a build (e.g. <c>dotnet build</c>) on some project.</summary>
    Build,

    /// <summary>Result of running a test slice.</summary>
    Test,

    /// <summary>Result of running static analysis / linting.</summary>
    Analysis,

    /// <summary>An action an agent took that other agents may want to know about (file written, PR opened, etc.).</summary>
    AgentAction,

    /// <summary>An external signal (operator command, mode change, manual intervention).</summary>
    UserSignal
}

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

