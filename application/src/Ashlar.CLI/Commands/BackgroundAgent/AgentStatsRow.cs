using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.BackgroundAgents.Telemetry;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// One row of aggregated cycle stats per agent.
/// </summary>
public sealed record AgentStatsRow(
    string agent,
    string role,
    int cycles,
    int successes,
    int failures,
    double avgDurationMs,
    int toolsExecuted,
    int toolsDenied,
    double avgIterations,
    DateTimeOffset lastCycleUtc,
    IReadOnlyDictionary<string, int> stoppedReasons);
