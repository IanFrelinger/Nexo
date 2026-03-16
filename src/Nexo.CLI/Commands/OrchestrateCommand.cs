using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Barriers;
using Nexo.CLI.Formatting;
using Nexo.CLI.Runtime;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Models;
using Nexo.Orchestration.Barriers;
using Nexo.Orchestration.Routing;
using Nexo.Runtime.Barriers;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for orchestrating agent execution.
/// 
/// Provides the `nexo orchestrate` command that:
/// - Takes a user request as input
/// - Delegates to Orchestrator for full workflow execution
/// - Displays progress and results in human-readable or JSON format
/// - Handles errors and provides appropriate exit codes
/// 
/// Part of the CLI layer, following the command pattern for user interactions.
/// </summary>
public class OrchestrateCommand
{
    private readonly Orchestrator _orchestrator;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<OrchestrateCommand> _logger;
    private readonly IOrchestrationRuntimeSpecAccessor _runtime;
    private readonly IEphemeralModelLifecycle? _ephemeralLifecycle;
    private readonly IBarrierContextAccessor _barrierContextAccessor;
    private readonly BarrierHierarchy _barrierHierarchy;
    private readonly BarrierOptions _barrierOptions;
    private readonly IBarrierAuditLog _barrierAuditLog;

    public OrchestrateCommand(
        Orchestrator orchestrator,
        IConsoleRenderer renderer,
        ILogger<OrchestrateCommand> logger,
        IOrchestrationRuntimeSpecAccessor runtime,
        IBarrierContextAccessor barrierContextAccessor,
        BarrierHierarchy barrierHierarchy,
        IOptions<BarrierOptions> barrierOptions,
        IBarrierAuditLog barrierAuditLog,
        IEphemeralModelLifecycle? ephemeralLifecycle = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _barrierContextAccessor = barrierContextAccessor ?? throw new ArgumentNullException(nameof(barrierContextAccessor));
        _barrierHierarchy = barrierHierarchy ?? throw new ArgumentNullException(nameof(barrierHierarchy));
        _barrierOptions = barrierOptions?.Value ?? throw new ArgumentNullException(nameof(barrierOptions));
        _barrierAuditLog = barrierAuditLog ?? throw new ArgumentNullException(nameof(barrierAuditLog));
        _ephemeralLifecycle = ephemeralLifecycle;
    }

    /// <summary>
    /// Executes the orchestrate command.
    /// </summary>
    /// <param name="request">User request to orchestrate</param>
    /// <param name="barrierLevel">Optional barrier level override.</param>
    /// <param name="preferredRegion">Optional preferred routing region override.</param>
    /// <param name="json">Whether to output JSON format</param>
    /// <param name="verbose">Whether to show verbose progress output</param>
    /// <returns>Exit code (0 for success, non-zero for errors)</returns>
    public Task<int> ExecuteAsync(string request, bool json, bool verbose)
        => ExecuteAsync(
            request,
            runtimeSpecPath: null,
            runtimeSpecJson: null,
            preferModel: null,
            provider: null,
            barrierLevel: null,
            preferredRegion: null,
            json,
            verbose);

    public async Task<int> ExecuteAsync(
        string request,
        string? runtimeSpecPath,
        string? runtimeSpecJson,
        string? preferModel,
        string? provider,
        string? barrierLevel,
        string? preferredRegion,
        bool json,
        bool verbose)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        if (verbose)
        {
            _renderer.RenderProgressStart($"CorrelationId={correlationId} :: orchestrating request: {request}");
        }

        try
        {
            var spec = OrchestrationRuntimeSpecLoader.Load(runtimeSpecPath, runtimeSpecJson);
            if (!string.IsNullOrWhiteSpace(preferModel))
            {
                spec = spec with { Model = spec.Model with { Prefer = preferModel!.Trim() } };
            }
            if (!string.IsNullOrWhiteSpace(provider))
            {
                spec = spec with { Model = spec.Model with { Provider = provider!.Trim() } };
            }
            if (!string.IsNullOrWhiteSpace(barrierLevel))
            {
                spec = spec with { BarrierLevel = barrierLevel.Trim() };
            }
            if (!string.IsNullOrWhiteSpace(preferredRegion))
            {
                spec = spec with { PreferredRegion = preferredRegion.Trim() };
            }

            await InitializeBarrierContextAsync(
                barrierLevel: spec.BarrierLevel,
                correlationId: correlationId,
                cancellationToken: CancellationToken.None);

            var modelPrefer = spec.Model?.Prefer;
            OrchestrationResult result;
            if (_ephemeralLifecycle != null)
            {
                await using var session = await _ephemeralLifecycle.StartSessionAsync(modelPrefer);
                using var _ = _runtime.Begin(spec);
                result = await _orchestrator.OrchestrateAsync(request);
            }
            else
            {
                using var _ = _runtime.Begin(spec);
                result = await _orchestrator.OrchestrateAsync(request);
            }

            if (json)
            {
                var jsonData = new
                {
                    ok = result.Success,
                    correlationId,
                    data = new
                    {
                        success = result.Success,
                        agentCount = result.Decomposition?.Agents.Count ?? 0,
                        conflicts = result.Conflicts.Count,
                        escalations = result.Escalations.Count,
                        progress = result.ProgressSummary != null ? new
                        {
                            completed = result.ProgressSummary.Completed,
                            total = result.ProgressSummary.TotalAgents,
                            percentage = result.ProgressSummary.ProgressPercentage
                        } : null,
                        output = result.IntegratedOutput?.IntegratedResults
                    }
                };
                _renderer.RenderJson(jsonData);
            }
            else
            {
                _renderer.RenderOrchestrationResult(result);
            }

            if (verbose)
            {
                _renderer.RenderProgressComplete($"CorrelationId={correlationId} :: orchestration completed");
            }

            return result.Success ? (int)ExitCode.Ok : (int)ExitCode.ValidationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed");
            var errorCode = ex switch
            {
                BarrierContextMissingException b => b.ErrorCode,
                BarrierElevationException b => b.ErrorCode,
                BarrierCeilingExceededException b => b.ErrorCode,
                EndpointUnavailableException => "ENDPOINT_UNAVAILABLE",
                _ => "UNEXPECTED_ERROR"
            };
            if (!json)
            {
                _renderer.RenderError($"Orchestration failed: {ex.Message}");
            }
            else
            {
                _renderer.RenderJson(new
                {
                    ok = false,
                    correlationId,
                    error = ex.Message,
                    errorCode
                });
            }
            return (int)ExitCode.UnexpectedError;
        }
    }

    private async Task InitializeBarrierContextAsync(
        string? barrierLevel,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barrierLevel))
        {
            if (_barrierOptions.RequireExplicitBarrier)
                throw new BarrierContextMissingException("*", correlationId);

            var defaultContext = BarrierContext.Create(
                _barrierHierarchy.Floor.Name,
                BarrierAuthoritySource.Default,
                "*",
                correlationId,
                _barrierHierarchy);

            _barrierContextAccessor.Initialize(defaultContext);
            await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.DefaultApplied,
                defaultContext.Level,
                defaultContext.AuthoritySource,
                "*",
                correlationId,
                string.Empty,
                DateTimeOffset.UtcNow,
                "No explicit barrier set at request boundary; defaulted to floor level."),
                cancellationToken);
            await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
                BarrierAuditEventType.ContextInitialized,
                defaultContext.Level,
                defaultContext.AuthoritySource,
                defaultContext.IssuedTo,
                correlationId,
                string.Empty,
                DateTimeOffset.UtcNow),
                cancellationToken);
            return;
        }

        var context = BarrierContext.Create(
            barrierLevel,
            BarrierAuthoritySource.Cli,
            "*",
            correlationId,
            _barrierHierarchy);

        _barrierContextAccessor.Initialize(context);
        await _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
            BarrierAuditEventType.ContextInitialized,
            context.Level,
            context.AuthoritySource,
            context.IssuedTo,
            correlationId,
            string.Empty,
            DateTimeOffset.UtcNow),
            cancellationToken);
    }
}

