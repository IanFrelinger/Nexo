using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Observations;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Reads the structured observations log (<c>observations.jsonl</c>) and prints
/// either the most recent rows or a per-source/per-kind summary. Designed as a
/// quick operator triage tool that complements <c>nexo background-agent stats</c>:
/// where <c>stats</c> answers "did the daemon run?", <c>observations</c> answers
/// "what did the daemon collectively learn?".
///
/// <para>This command is read-only and offline. It never starts the daemon, calls
/// the registry, or hits an LLM, so it is safe to run alongside a live daemon.</para>
/// </summary>
public class ObservationsBackgroundAgentCommand
{
    private readonly IObservationStore _store;
    private readonly ILogger<ObservationsBackgroundAgentCommand> _logger;

    /// <summary>Creates a new ObservationsBackgroundAgentCommand instance.</summary>
    public ObservationsBackgroundAgentCommand(
        IObservationStore store,
        ILogger<ObservationsBackgroundAgentCommand> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// CLI entry point that writes to <see cref="Console.Out"/>. Tests should call
    /// the writer-based overload to avoid clobbering the shared console.
    /// </summary>
    public Task<int> ExecuteAsync(
        string? source,
        string? kind,
        double? sinceHours,
        int? tail,
        bool summary,
        bool formatJson,
        CancellationToken ct = default)
        => ExecuteAsync(source, kind, sinceHours, tail, summary, formatJson, Console.Out, Console.Error, ct);

    /// <summary>
    /// Test-friendly overload that writes to caller-supplied streams instead of
    /// <see cref="Console.Out"/> / <see cref="Console.Error"/>.
    /// </summary>
    public Task<int> ExecuteAsync(
        string? source,
        string? kind,
        double? sinceHours,
        int? tail,
        bool summary,
        bool formatJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct = default)
    {
        try
        {
            var cutoff = sinceHours is > 0
                ? DateTimeOffset.UtcNow - TimeSpan.FromHours(sinceHours.Value)
                : (DateTimeOffset?)null;

            ObservationKind? kindFilter = null;
            if (!string.IsNullOrWhiteSpace(kind))
            {
                if (!Enum.TryParse<ObservationKind>(kind, ignoreCase: true, out var parsed))
                {
                    var allowed = string.Join(", ", Enum.GetNames<ObservationKind>());
                    var msg = $"Unknown --kind '{kind}'. Allowed: {allowed}.";
                    if (formatJson)
                        stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = msg }));
                    else
                        stderr.WriteLine(msg);
                    return Task.FromResult(2);
                }
                kindFilter = parsed;
            }

            var rows = _store.ReadSince(since: cutoff, kind: kindFilter)
                .Where(o => string.IsNullOrWhiteSpace(source) ||
                            string.Equals(o.source, source, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Tail is applied last so the user always sees the N most recent rows
            // matching the filters, not the first N (oldest first).
            if (tail is > 0 && rows.Count > tail.Value)
                rows = rows.GetRange(rows.Count - tail.Value, tail.Value);

            if (summary)
            {
                EmitSummary(rows, formatJson, stdout);
            }
            else
            {
                EmitRows(rows, formatJson, stdout);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Observations command failed");
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                stderr.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private void EmitRows(List<RuntimeObservation> rows, bool formatJson, TextWriter stdout)
    {
        if (formatJson)
        {
            var payload = new
            {
                ok = true,
                path = _store.Location,
                count = rows.Count,
                observations = rows
            };
            stdout.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        stdout.WriteLine($"Source: {_store.Location}");
        if (rows.Count == 0)
        {
            stdout.WriteLine("No matching observations.");
            return;
        }
        stdout.WriteLine($"Observations: {rows.Count}");
        stdout.WriteLine();
        stdout.WriteLine("Time(UTC)             Source                         Kind        Sev    Summary");
        stdout.WriteLine("--------------------- ------------------------------ ----------- ------ -------------------------------------------");
        foreach (var o in rows)
        {
            stdout.WriteLine(
                $"{o.ts:u} {Truncate(o.source, 30),-30} {o.kind,-11} {o.severity,-6} {Truncate(o.summary, 60)}");
        }
    }

    private void EmitSummary(List<RuntimeObservation> rows, bool formatJson, TextWriter stdout)
    {
        var bySource = rows
            .GroupBy(o => o.source, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SourceSummary(
                source: g.Key,
                total: g.Count(),
                byKind: g.GroupBy(o => o.kind).ToDictionary(k => k.Key.ToString(), k => k.Count()),
                bySeverity: g.GroupBy(o => o.severity).ToDictionary(k => k.Key.ToString(), k => k.Count()),
                lastSeen: g.Max(o => o.ts)))
            .ToList();

        if (formatJson)
        {
            var payload = new
            {
                ok = true,
                path = _store.Location,
                totalObservations = rows.Count,
                sources = bySource
            };
            stdout.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        stdout.WriteLine($"Source: {_store.Location}");
        if (rows.Count == 0)
        {
            stdout.WriteLine("No matching observations.");
            return;
        }
        stdout.WriteLine($"Observations: {rows.Count}");
        stdout.WriteLine();
        stdout.WriteLine("Source                         Total  Build  Test  Analysis  AgentAct  UserSig  Errors  LastSeen");
        stdout.WriteLine("------------------------------ ------ ------ ----- --------- --------- -------- ------ --------------------");
        foreach (var s in bySource)
        {
            int Pick(string k) => s.byKind.TryGetValue(k, out var v) ? v : 0;
            int errors = s.bySeverity.TryGetValue(nameof(ObservationSeverity.Error), out var e) ? e : 0;
            stdout.WriteLine(
                $"{Truncate(s.source, 30),-30} {s.total,6} {Pick("Build"),6} {Pick("Test"),5} {Pick("Analysis"),9} {Pick("AgentAction"),9} {Pick("UserSignal"),8} {errors,6} {s.lastSeen:u}");
        }
    }

    private static string Truncate(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= len ? s : s.Substring(0, len - 1) + "…";
    }

    private sealed record SourceSummary(
        string source,
        int total,
        IReadOnlyDictionary<string, int> byKind,
        IReadOnlyDictionary<string, int> bySeverity,
        DateTimeOffset lastSeen);
}
