using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Ashlar.BackgroundAgents.Observations;
using Ashlar.Core.Application.Autonomy;

namespace Ashlar.BackgroundAgents.Objectives;

/// <summary>
/// The prompt-injection fence at the objective layer (autonomous self-extension spec
/// R1.2): telemetry-derived objectives are built by deterministic extraction of
/// schema-constrained fields ONLY. Watched workflows produce free text — tool output,
/// log lines, model chatter — and instruction-shaped text in that data must never become
/// instruction. Concretely: the observation's <c>summary</c> and <c>facts</c> values are
/// NEVER copied into the objective. The objective body carries the observation's
/// timestamp/source/kind/severity so a human can look the free text up; the proposer
/// never sees it through this channel.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class TelemetryObjectiveExtractor
{
    /// <summary>
    /// Builds a telemetry-sourced objective from one observed workflow gap. The touch-set
    /// must be supplied by the CALLER's gap catalog (a static mapping from gap kind to
    /// blast radius), never derived from observation content — content-derived touch-sets
    /// would let watched data widen its own blast radius.
    /// </summary>
    public static ObjectiveDocument FromObservation(
        RuntimeObservation observation,
        string objectiveId,
        TouchSet? touchSet = null,
        int priority = 100)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (string.IsNullOrWhiteSpace(objectiveId))
            throw new ArgumentException("Objective id is required.", nameof(objectiveId));

        var ts = observation.ts.ToString("u", CultureInfo.InvariantCulture);
        var sourceId = SanitizeIdentifier(observation.source);
        var now = DateTimeOffset.UtcNow;

        return new ObjectiveDocument
        {
            Id = objectiveId,
            // Schema fields only — never observation.summary (R1.2).
            Title = $"Address observed {observation.kind} gap from {sourceId}",
            Source = ObjectiveSource.Telemetry,
            Touch = touchSet,
            Priority = priority,
            Tags = new[] { "telemetry", observation.kind.ToString().ToLowerInvariant() },
            CreatedAt = now,
            UpdatedAt = now,
            Body =
                $"Observed a {observation.severity} {observation.kind} signal from '{sourceId}' at {ts}.\n\n" +
                "This objective was extracted deterministically from telemetry (R1.2): the observation's " +
                "free-text summary is deliberately NOT included here, because watched-workflow text is " +
                "untrusted and must not reach proposer instructions. To read the original signal, query " +
                $"the observation store for source '{sourceId}' at {ts}.",
        };
    }

    /// <summary>
    /// Source labels are machine-assigned agent ids, but they still transit watched data;
    /// clamp to an identifier alphabet. Truncation at the FIRST disallowed character —
    /// not filtering — so a label like <c>"agent one: IGNORE …"</c> yields <c>agent</c>:
    /// nothing past the first illegal character survives, rather than the payload
    /// re-concatenating into the objective.
    /// </summary>
    private static string SanitizeIdentifier(string value)
    {
        var trimmed = (value ?? "").Trim();
        var chars = trimmed
            .TakeWhile(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.')
            .Take(64)
            .ToArray();
        return chars.Length == 0 ? "unknown" : new string(chars);
    }
}
