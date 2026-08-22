using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Telemetry;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// Aggregates the structured cycle event log (<c>cycles.jsonl</c>) into a quick
/// per-agent / per-role summary so operators can answer questions like:
/// <list type="bullet">
///   <item>"Is the planner actually running cycles, and how often?"</item>
///   <item>"What's the average cycle time and tool throughput per agent?"</item>
///   <item>"Which agents are denying tool calls (policy friction) vs. erroring?"</item>
/// </list>
///
/// <para>This command is read-only and offline — it never starts the daemon, talks to
/// the registry, or hits any LLM. It only reads the local JSONL file, so it's safe
/// to run alongside a live daemon.</para>
/// </summary>
public class StatsBackgroundAgentCommand
{
    private readonly CycleEventStore _store;
    private readonly ILogger<StatsBackgroundAgentCommand> _logger;

    /// <summary>Creates a new StatsBackgroundAgentCommand instance.</summary>
    public StatsBackgroundAgentCommand(
        CycleEventStore store,
        ILogger<StatsBackgroundAgentCommand> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Print aggregated stats. Optional filters narrow the read.
    /// </summary>
    /// <param name="agent">If set, only events whose <c>agent</c> matches (case-insensitive) are counted.</param>
    /// <param name="role">If set, only events whose <c>role</c> matches (case-insensitive) are counted.</param>
    /// <param name="sinceHours">If set and &gt; 0, only events newer than now-N hours are counted.</param>
    /// <param name="formatJson">When true, emit a single JSON document instead of a human table.</param>
    public Task<int> ExecuteAsync(
        string? agent,
        string? role,
        double? sinceHours,
        bool formatJson,
        CancellationToken ct = default)
        => ExecuteAsync(agent, role, sinceHours, formatJson, Console.Out, Console.Error, ct);

    /// <summary>
    /// Test-friendly overload that writes to caller-supplied streams instead of
    /// <see cref="Console.Out"/> / <see cref="Console.Error"/>. The CLI host always uses
    /// the public no-stream overload above; tests use this one to stay isolated when
    /// the suite runs in parallel.
    /// </summary>
    public Task<int> ExecuteAsync(
        string? agent,
        string? role,
        double? sinceHours,
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

            var events = _store.Read()
                .Where(e => string.IsNullOrWhiteSpace(agent) ||
                            string.Equals(e.agent, agent, StringComparison.OrdinalIgnoreCase))
                .Where(e => string.IsNullOrWhiteSpace(role) ||
                            string.Equals(e.role, role, StringComparison.OrdinalIgnoreCase))
                .Where(e => cutoff is null || e.ts >= cutoff)
                .ToList();

            var byAgent = events
                .GroupBy(e => e.agent, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => Aggregate(g.Key, g.First().role, g))
                .ToList();

            if (formatJson)
            {
                var payload = new
                {
                    ok = true,
                    path = _store.Path,
                    filters = new { agent, role, sinceHours },
                    totalEvents = events.Count,
                    agents = byAgent
                };
                stdout.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                stdout.WriteLine($"Source: {_store.Path}");
                if (events.Count == 0)
                {
                    stdout.WriteLine("No matching cycle events.");
                    return Task.FromResult(0);
                }
                stdout.WriteLine($"Events: {events.Count}");
                stdout.WriteLine();
                stdout.WriteLine("Agent                          Role         Cycles   Ok   Fail   Avg(ms)  Tools  Denied  LastCycle");
                stdout.WriteLine("------------------------------ ------------ ------- ---- ------ -------- ------ ------- --------------------");
                foreach (var a in byAgent)
                {
                    stdout.WriteLine(
                        $"{Truncate(a.agent, 30),-30} {Truncate(a.role, 12),-12} {a.cycles,7} {a.successes,4} {a.failures,6} {a.avgDurationMs,8:F0} {a.toolsExecuted,6} {a.toolsDenied,7} {a.lastCycleUtc:u}");
                }
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stats command failed");
            if (formatJson)
                stdout.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                stderr.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static AgentStatsRow Aggregate(string agent, string role, IEnumerable<CycleEvent> events)
    {
        var list = events.ToList();
        var cycles = list.Count;
        var successes = list.Count(e => e.success);
        var failures = cycles - successes;
        var avgDuration = cycles == 0 ? 0d : list.Average(e => (double)e.duration_ms);
        var toolsExecuted = list.Sum(e => e.tools_executed);
        var toolsDenied = list.Sum(e => e.tools_denied);
        var totalIterations = list.Sum(e => e.iterations ?? 0);
        var avgIterations = cycles == 0 ? 0d : (double)totalIterations / cycles;
        var lastCycle = list.Max(e => e.ts);
        var stoppedReasons = list
            .Where(e => !string.IsNullOrEmpty(e.stopped_reason))
            .GroupBy(e => e.stopped_reason!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        return new AgentStatsRow(
            agent,
            role,
            cycles,
            successes,
            failures,
            avgDuration,
            toolsExecuted,
            toolsDenied,
            avgIterations,
            lastCycle,
            stoppedReasons);
    }

    private static string Truncate(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= len ? s : s.Substring(0, len - 1) + "…";
    }
}
