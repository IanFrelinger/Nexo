using Nexo.BackgroundAgents.Configuration;

namespace Nexo.BackgroundAgents.Playtesting;

/// <summary>Runs one configured external game playtest cycle.</summary>
public interface IPlaytestRunRunner
{
    Task<PlaytestRunResult> RunAsync(
        BackgroundAgentConfig config,
        CancellationToken cancellationToken = default);
}

/// <summary>Structured result published by the background-agent registry.</summary>
public sealed record PlaytestRunResult(
    bool Success,
    string Summary,
    string? ReportPath,
    int ActionsExecuted,
    IReadOnlyDictionary<string, string>? Facts = null);
