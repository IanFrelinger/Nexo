using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.BackgroundAgents.Extending;
using Nexo.Runtime;

namespace Nexo.CLI.Commands.BackgroundAgent;

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
    {
        if (!BackgroundAgentAdapterValidation.TryResolveDirectory(repoRoot, "RepoRoot", out var errorMessage))
        {
            return new SelfExtendRunResult(false, 0, 0, errorMessage!);
        }

        try
        {
            var (tools, policies) = RepoFsToolboxFactory.CreateMinimal();

            var agent = new ToolCallingAgent("self-extend", _model, _loggerFactory.CreateLogger<ToolCallingAgent>());
            var snapshot = WorldSnapshot.ForRepo(repoRoot!);

            var host = new AgentHost(new[] { agent }, tools, policies);
            var delta = await host.StepAsync(snapshot, cancellationToken).ConfigureAwait(false);

            var executed = delta?.Log.Count(l =>
                l.StartsWith("write:", StringComparison.Ordinal) ||
                l.StartsWith("s&r:", StringComparison.Ordinal)) ?? 0;
            var denied = 0;
            var summary = delta == null
                ? "No tool calls executed."
                : $"{executed} tool call(s) executed, {denied} denied.";
            _logger.LogDebug("Self-extend cycle: {Summary}", summary);
            return new SelfExtendRunResult(denied == 0, executed, denied, summary);
        }
        catch (Exception ex)
        {
            return new SelfExtendRunResult(false, 0, 0, BackgroundAgentAdapterFailure.LogAndMessage(_logger, ex, $"Run failed: {ex.Message}"));
        }
    }
}
