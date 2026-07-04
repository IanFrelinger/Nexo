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
/// namespace (the OS-level event/pattern pipeline) and the orchestration-layer
/// <c>AgentObservation</c> type would otherwise collide on short names.</para>
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
