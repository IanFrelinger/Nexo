using System.Globalization;
using System.Text;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// One Markdown line of planner history. Captured at the end of each ReAct cycle.
/// </summary>
public sealed record ScratchpadEntry(
    DateTimeOffset Timestamp,
    string AgentName,
    int Iterations,
    int ToolsExecuted,
    int ToolsDenied,
    string? StoppedReason,
    string? Rationale,
    IReadOnlyList<string>? WritePaths);
