using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.BackgroundAgents.Extending;
using Nexo.Runtime;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Host implementation of ISelfExtendRunner: builds a toolbox (repo.fs.write, repo.fs.search_replace),
/// policy (path allowlist, max write size), and a tool-calling agent backed by IModel, then runs one ThinkAsync cycle and executes approved tool calls.
/// </summary>
public sealed class SelfExtendRunnerAdapter : ISelfExtendRunner
{
    private readonly IModel _model;
    private readonly ILogger<SelfExtendRunnerAdapter> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public SelfExtendRunnerAdapter(
        IModel model,
        ILogger<SelfExtendRunnerAdapter> logger,
        ILoggerFactory loggerFactory)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public async Task<SelfExtendRunResult> RunAsync(string repoRoot, CancellationToken cancellationToken = default)
        => await RunAsync(repoRoot, objective: null, agentName: null, modelProvider: null, modelName: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Runs a self-extend cycle with an explicit objective for the tool-calling agent.
    /// </summary>
    public async Task<SelfExtendRunResult> RunAsync(string repoRoot, string? objective, CancellationToken cancellationToken = default)
        => await RunAsync(repoRoot, objective, agentName: null, modelProvider: null, modelName: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Runs a self-extend cycle with an explicit objective and agent role name for multi-agent workflows.
    /// </summary>
    public async Task<SelfExtendRunResult> RunAsync(string repoRoot, string? objective, string? agentName, CancellationToken cancellationToken = default)
        => await RunAsync(repoRoot, objective, agentName, modelProvider: null, modelName: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Runs a self-extend cycle with an explicit objective, agent role name, and model routing.
    /// Implements <see cref="ISelfExtendRunner.RunAsync(string, string?, string?, string?, string?, CancellationToken)"/>.
    /// </summary>
    public async Task<SelfExtendRunResult> RunAsync(
        string repoRoot,
        string? objective,
        string? agentName,
        string? modelProvider,
        string? modelName,
        CancellationToken cancellationToken = default)
    {
        if (!BackgroundAgentAdapterValidation.TryResolveDirectory(repoRoot, "RepoRoot", out var errorMessage))
        {
            return new SelfExtendRunResult(false, 0, 0, errorMessage!);
        }

        try
        {
            var (tools, policies) = RepoFsToolboxFactory.CreateMinimal();

            var resolvedAgentName = string.IsNullOrWhiteSpace(agentName) ? "self-extend" : agentName.Trim();
            var agent = new ToolCallingAgent(
                resolvedAgentName,
                _model,
                _loggerFactory.CreateLogger<ToolCallingAgent>(),
                objective,
                modelProvider,
                modelName);
            var snapshot = BuildSnapshot(repoRoot!, resolvedAgentName, objective);

            var deniedCount = 0;
            ChainRejectionCallback onRejected = (_, _, _) => Interlocked.Increment(ref deniedCount);

            var host = new AgentHost(new[] { agent }, tools, policies, onRejected);
            var delta = await host.StepAsync(snapshot, cancellationToken).ConfigureAwait(false);

            // Count every tool log line emitted by the toolbox (read/list/write/search-replace) —
            // not just mutating ones. Inspection calls are real, useful work and should be reflected
            // in the cycle summary so operators can tell "the agent thought and inspected the repo"
            // apart from "the agent silently no-op'd".
            var executed = delta?.Log.Count(l =>
                l.StartsWith("read:", StringComparison.Ordinal) ||
                l.StartsWith("list:", StringComparison.Ordinal) ||
                l.StartsWith("write:", StringComparison.Ordinal) ||
                l.StartsWith("s&r:", StringComparison.Ordinal)) ?? 0;
            var summary = delta == null && deniedCount == 0
                ? "No tool calls executed."
                : $"{executed} tool call(s) executed, {deniedCount} denied.";
            _logger.LogInformation("Self-extend cycle ({Agent}): {Summary}", resolvedAgentName, summary);
            return new SelfExtendRunResult(deniedCount == 0, executed, deniedCount, summary);
        }
        catch (Exception ex)
        {
            return new SelfExtendRunResult(false, 0, 0, BackgroundAgentAdapterFailure.LogAndMessage(_logger, ex, $"Run failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Builds the snapshot passed to the tool-calling agent. In addition to the standard
    /// RepoRoot/OutputRoot, this includes:
    /// - <c>AgentName</c>, <c>Objective</c>: surfaced so the LLM can ground its plan even when
    ///   the system prompt is heavily token-truncated.
    /// - <c>RepoOverview</c>: top-level directory + key files (README, AGENTS, etc.) so the
    ///   model has bootstrap context without having to call <c>repo.fs.list .</c> first. With
    ///   the previous bare snapshot the planner was being asked to write blind.
    /// </summary>
    private static WorldSnapshot BuildSnapshot(string repoRoot, string agentName, string? objective)
    {
        var data = new Dictionary<string, object?>
        {
            ["RepoRoot"] = repoRoot,
            ["OutputRoot"] = Path.Combine(repoRoot, "out"),
            ["AgentName"] = agentName
        };

        if (!string.IsNullOrWhiteSpace(objective))
            data["Objective"] = objective.Trim();

        try
        {
            var rootFull = Path.GetFullPath(repoRoot);
            var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin", "obj", ".git", ".vs", "node_modules", ".nuget", ".idea", ".cache", "out"
            };

            var topDirs = Directory
                .EnumerateDirectories(rootFull)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n) && !skipDirs.Contains(n!))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToArray();

            var keyFileCandidates = new[]
            {
                "README.md", "AGENTS.md", "Directory.Build.props", "Directory.Packages.props",
                "global.json", "Nexo.sln", "package.json"
            };
            var topFiles = keyFileCandidates
                .Where(f => File.Exists(Path.Combine(rootFull, f)))
                .ToArray();

            data["RepoOverview"] = new
            {
                root = rootFull,
                topDirectories = topDirs,
                topFiles
            };
        }
        catch
        {
            // Bootstrap context is best-effort; falling back to bare RepoRoot is fine.
        }

        return new WorldSnapshot(0, data);
    }
}
