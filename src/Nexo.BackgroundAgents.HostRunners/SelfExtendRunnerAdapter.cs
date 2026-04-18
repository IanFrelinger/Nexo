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
            var scratchpadPath = ResolveScratchpadPath(repoRoot!, resolvedAgentName);
            var snapshot = BuildSnapshot(repoRoot!, resolvedAgentName, objective, scratchpadPath);

            // Multi-turn ReAct: agent loops up to MaxIterations, executing tool calls inline
            // through the policy engine and feeding observations back into the conversation.
            // The previous single-turn AgentHost path was unable to chain list → read → write
            // because tool results never reached the LLM after the first round.
            var memory = tools.MemoryFor(agent);
            var cycle = await agent
                .RunCycleAsync(snapshot, tools, policies, onRejected: null, memory, cancellationToken)
                .ConfigureAwait(false);

            var writePaths = cycle.MergedDelta is null
                ? Array.Empty<string>()
                : ExtractWritePaths(cycle.MergedDelta.Log);
            PlannerScratchpad.Append(scratchpadPath, new ScratchpadEntry(
                DateTimeOffset.UtcNow,
                resolvedAgentName,
                cycle.Iterations,
                cycle.ToolCallsExecuted,
                cycle.ToolCallsDenied,
                cycle.StoppedReason,
                cycle.FinalRationale,
                writePaths));

            var summary = $"{cycle.ToolCallsExecuted} tool call(s) executed, {cycle.ToolCallsDenied} denied " +
                          $"(iter={cycle.Iterations}, stopped={cycle.StoppedReason}).";
            _logger.LogInformation("Self-extend cycle ({Agent}): {Summary}", resolvedAgentName, summary);
            return new SelfExtendRunResult(cycle.ToolCallsDenied == 0, cycle.ToolCallsExecuted, cycle.ToolCallsDenied, summary);
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
    private static WorldSnapshot BuildSnapshot(string repoRoot, string agentName, string? objective, string? scratchpadPath = null)
    {
        var data = new Dictionary<string, object?>
        {
            ["RepoRoot"] = repoRoot,
            ["OutputRoot"] = Path.Combine(repoRoot, "out"),
            ["AgentName"] = agentName
        };

        if (!string.IsNullOrWhiteSpace(objective))
            data["Objective"] = objective.Trim();

        if (!string.IsNullOrWhiteSpace(scratchpadPath))
        {
            var tail = PlannerScratchpad.LoadTail(scratchpadPath);
            if (!string.IsNullOrWhiteSpace(tail))
                data["RecentNotes"] = tail;
        }

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

    /// <summary>
    /// Default scratchpad location: <c>&lt;repoRoot&gt;/.nexo/runtime-studio/&lt;agent&gt;-notes.md</c>.
    /// The directory is created lazily by <see cref="PlannerScratchpad.Append"/>; the agent name
    /// is sanitised so that values like "runtime-planner" or "self-extend" produce predictable
    /// filenames without filesystem-illegal characters.
    /// </summary>
    private static string ResolveScratchpadPath(string repoRoot, string agentName)
    {
        var safe = string.Concat(agentName.Select(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-'));
        return Path.Combine(repoRoot, ".nexo", "runtime-studio", $"{safe}-notes.md");
    }

    private static IReadOnlyList<string> ExtractWritePaths(IReadOnlyList<string> log)
    {
        // Both repo.fs.write and repo.fs.search_replace tools log entries shaped like
        // "write:relative/path bytes=NNN" / "s&r:relative/path …". Pull the first
        // whitespace-bounded token after the prefix so the scratchpad records what
        // actually changed on disk.
        var result = new List<string>();
        foreach (var line in log)
        {
            string? prefix = null;
            if (line.StartsWith("write:", StringComparison.Ordinal)) prefix = "write:";
            else if (line.StartsWith("s&r:", StringComparison.Ordinal)) prefix = "s&r:";
            if (prefix is null) continue;
            var rest = line[prefix.Length..];
            var space = rest.IndexOf(' ');
            var path = space < 0 ? rest : rest[..space];
            if (!string.IsNullOrWhiteSpace(path))
                result.Add(path);
        }
        return result;
    }
}
