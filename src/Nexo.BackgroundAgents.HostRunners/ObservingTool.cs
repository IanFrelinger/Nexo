using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Observations;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Decorates an <see cref="ITool"/> so that every successful invocation also
/// emits a <see cref="RuntimeObservation"/> to the configured store. Used to
/// surface build/test outcomes that the planner triggered itself, without
/// coupling tool implementations to <see cref="IObservationStore"/>.
///
/// <para>Observation emission is best-effort and never propagates exceptions
/// — tool execution semantics must be unaffected by telemetry I/O.</para>
/// </summary>
internal sealed class ObservingTool : ITool
{
    private readonly ITool _inner;
    private readonly IObservationStore _store;
    private readonly Func<ToolCall, ToolResult, RuntimeObservation?> _projector;

    public ObservingTool(
        ITool inner,
        IObservationStore store,
        Func<ToolCall, ToolResult, RuntimeObservation?> projector)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
    }

    public string Id => _inner.Id;
    public ToolSchema Schema => _inner.Schema;

    public async Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var result = await _inner.InvokeAsync(call, s, ct).ConfigureAwait(false);
        try
        {
            var observation = _projector(call, result);
            if (observation is not null)
                _store.Append(observation);
        }
        catch
        {
            // Observation emission must never destabilise tool execution.
        }
        return result;
    }

    /// <summary>
    /// Standard projector for <c>dotnet.build</c> / <c>dotnet.test</c>: looks for
    /// the conventional <c>{ ok, stdout, stderr }</c> payload shape and produces
    /// a Build/Test observation tagged with success and (for tests) the parsed
    /// <c>Passed!/Failed!</c> tail. Returns <c>null</c> if the payload doesn't
    /// match — keeping observation noise out of unrelated tools.
    /// </summary>
    public static Func<ToolCall, ToolResult, RuntimeObservation?> DotnetProjector(
        string source,
        ObservationKind kind)
    {
        return (call, result) =>
        {
            if (result.Payload is null) return null;
            var json = JsonSerializer.Serialize(result.Payload);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!doc.RootElement.TryGetProperty("ok", out var okEl)) return null;

            var ok = okEl.ValueKind == JsonValueKind.True;
            var stdout = doc.RootElement.TryGetProperty("stdout", out var so) ? so.GetString() ?? "" : "";
            var stderr = doc.RootElement.TryGetProperty("stderr", out var se) ? se.GetString() ?? "" : "";
            var summary = SummarizeDotnetOutput(kind, ok, stdout, stderr);

            var facts = new Dictionary<string, string>
            {
                ["ok"] = ok ? "true" : "false",
                ["tool"] = call.Id
            };
            if (call.Arguments.ValueKind == JsonValueKind.Object &&
                call.Arguments.TryGetProperty("root", out var root) &&
                root.ValueKind == JsonValueKind.String)
            {
                facts["root"] = root.GetString() ?? string.Empty;
            }

            return new RuntimeObservation(
                ts: DateTimeOffset.UtcNow,
                source: source,
                kind: kind,
                summary: summary,
                severity: ok ? ObservationSeverity.Info : ObservationSeverity.Error,
                facts: facts);
        };
    }

    private static string SummarizeDotnetOutput(ObservationKind kind, bool ok, string stdout, string stderr)
    {
        // We deliberately keep the summary short (<= ~200 chars). The full stdout/stderr
        // is already in the tool's ToolResult payload that the LLM saw; the observation
        // is a *signal* for downstream cycles, not a transcript.
        var prefix = kind == ObservationKind.Build ? "Build" : "Tests";
        if (ok) return $"{prefix} succeeded";

        var line = ExtractRelevantLine(stdout)
                   ?? ExtractRelevantLine(stderr)
                   ?? "(no diagnostic line captured)";
        if (line.Length > 200) line = line[..200];
        return $"{prefix} failed: {line}";
    }

    private static string? ExtractRelevantLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        // Look for the canonical dotnet failure summary line first.
        foreach (var line in text.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0) continue;
            if (t.StartsWith("Failed!", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("Build FAILED", StringComparison.OrdinalIgnoreCase) ||
                t.Contains(": error ", StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
        }
        // Fallback: the last non-empty line is usually the summary.
        var lines = text.Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var t = lines[i].Trim();
            if (!string.IsNullOrEmpty(t)) return t;
        }
        return null;
    }
}
