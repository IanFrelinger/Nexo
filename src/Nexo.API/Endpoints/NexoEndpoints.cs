using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexo.BrickContracts.Capabilities;
using Nexo.Core.Application.Copilot.Models;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Knowledge.Models;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.Infrastructure.Testing.ExecutionPlatform;
using Nexo.API.Security;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Nexo.API.Endpoints;

/// <summary>
/// Extension methods for mapping Nexo API endpoints.
/// </summary>
public static class NexoEndpoints
{
    private static readonly JsonSerializerOptions DailySerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static IEndpointRouteBuilder MapNexoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Nexo");

        group.MapPost("/agent", RunAgentAsync)
            .WithName("RunAgent")
            .WithSummary("Invoke an agent by name")
            .Produces<AgentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/validate", RunValidationAsync)
            .WithName("RunValidation")
            .WithSummary("Run validation tests")
            .Produces<ValidationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/orchestrate", OrchestrateAsync)
            .WithName("Orchestrate")
            .WithSummary("Run orchestration workflow")
            .Produces<OrchestrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/copilot/task", RunCopilotTaskAsync)
            .WithName("RunCopilotTask")
            .WithSummary("Run copilot task and return trust-auditable context")
            .Produces<CopilotTaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/copilot/tasks", ListCopilotTasksAsync)
            .WithName("ListCopilotTasks")
            .WithSummary("List recent copilot tasks (newest first)")
            .Produces<IReadOnlyList<CopilotTaskRecord>>(StatusCodes.Status200OK);

        group.MapGet("/status", GetStatusAsync)
            .WithName("GetStatus")
            .WithSummary("Get background agent status and mode")
            .Produces<StatusResponse>(StatusCodes.Status200OK);

        group.MapPost("/execution/build", BuildImageAsync)
            .WithName("BuildImage")
            .WithSummary("Build a container image from Dockerfile (for RemoteExecutionPlatform)")
            .Produces<ExecutionBuildResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/execution/run", RunContainerAsync)
            .WithName("RunContainer")
            .WithSummary("Run a container (for RemoteExecutionPlatform)")
            .Produces<ExecutionRunResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/capabilities", GetCapabilitiesAsync)
            .WithName("GetCapabilities")
            .WithSummary("Get node capability manifest for brick routing")
            .Produces<NodeCapabilityManifestDto>(StatusCodes.Status200OK);

        group.MapGet("/security/advisory", GetSecurityAdvisory)
            .WithName("GetSecurityAdvisory")
            .WithSummary("Operator exposure profile and hints (user-configured; advisory only)")
            .Produces<SecurityAdvisoryResponse>(StatusCodes.Status200OK);

        group.MapGet("/trust/status", GetTrustStatusAsync)
            .WithName("GetTrustStatus")
            .WithSummary("Get trust boundary status and active trust policy pack")
            .Produces<TrustStatusResponse>(StatusCodes.Status200OK);

        group.MapPost("/director/run", RunDirectorWorkflowAsync)
            .WithName("RunDirectorWorkflow")
            .WithSummary("Run one directorial iteration and persist as a daily")
            .Produces<DirectorRunResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/director/dailies", ListDailiesAsync)
            .WithName("ListDirectorDailies")
            .WithSummary("List persisted directorial dailies")
            .Produces<List<DirectorDailySummary>>(StatusCodes.Status200OK);

        group.MapGet("/director/dailies/{dailyId}", GetDailyAsync)
            .WithName("GetDirectorDaily")
            .WithSummary("Get one persisted directorial daily")
            .Produces<DirectorDailyEntry>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/background-agents/summary", GetBackgroundAgentSummaryAsync)
            .WithName("GetBackgroundAgentSummary")
            .WithSummary("Get background agent health summary")
            .Produces<BackgroundAgentSummaryResponse>(StatusCodes.Status200OK);

        group.MapGet("/trust/dashboard", GetTrustDashboardAsync)
            .WithName("GetTrustDashboard")
            .WithSummary("Get trust boundary and recent audit events")
            .Produces<TrustDashboardResponse>(StatusCodes.Status200OK);

        group.MapPost("/trust/pause", SetTrustPauseAsync)
            .WithName("SetTrustPause")
            .WithSummary("Pause or resume trust observation boundary")
            .Produces<TrustBoundaryMutationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/trust/rule", SetTrustRuleAsync)
            .WithName("SetTrustRule")
            .WithSummary("Update trust allow/deny rules for category/source")
            .Produces<TrustBoundaryMutationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/knowledge/query", QueryKnowledgeAsync)
            .WithName("QueryKnowledge")
            .WithSummary("Query unified adaptation/pattern/knowledge timeline")
            .Produces<KnowledgeQueryResult>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> RunAgentAsync(
        [FromBody] AgentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.AgentName))
            return Results.BadRequest(new ProblemDetails { Title = "AgentName is required" });

        try
        {
            var command = new RunAgentCommand(request.AgentName, request.InputFilePath != null ? new FileInfo(request.InputFilePath) : null);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(new AgentResponse(result.Success, result.Message, result.Output));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RunValidationAsync(
        [FromBody] ValidationRequest? request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RunValidationCommand(request?.Filter);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(new ValidationResponse(result.Passed, result.Message, result.TestsRun, result.TestsPassed, result.TestsFailed));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> OrchestrateAsync(
        [FromBody] OrchestrationRequest request,
        [FromServices] Orchestrator orchestrator,
        [FromServices] IOrchestrationRuntimeSpecAccessor? runtimeSpecAccessor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Request))
            return Results.BadRequest(new ProblemDetails { Title = "Request is required" });

        try
        {
            OrchestrationRuntimeSpec spec;
            if (!string.IsNullOrWhiteSpace(request.RuntimeSpecJson))
            {
                try
                {
                    spec = JsonSerializer.Deserialize<OrchestrationRuntimeSpec>(
                               request.RuntimeSpecJson,
                               new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? OrchestrationRuntimeSpec.Default();
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "runtimeSpecJson is invalid",
                        Detail = ex.Message
                    });
                }
            }
            else
            {
                spec = OrchestrationRuntimeSpec.Default();
            }

            if (!string.IsNullOrWhiteSpace(request.PreferModel))
                spec = spec with { Model = spec.Model with { Prefer = request.PreferModel.Trim() } };
            if (!string.IsNullOrWhiteSpace(request.Provider))
                spec = spec with { Model = spec.Model with { Provider = request.Provider.Trim() } };
            if (!string.IsNullOrWhiteSpace(request.BarrierLevel))
                spec = spec with { BarrierLevel = request.BarrierLevel.Trim() };
            if (!string.IsNullOrWhiteSpace(request.PreferredRegion))
                spec = spec with { PreferredRegion = request.PreferredRegion.Trim() };

            using var _ = runtimeSpecAccessor?.Begin(spec);
            var result = await orchestrator.OrchestrateAsync(request.Request, cancellationToken);
            return Results.Ok(new OrchestrationResponse(
                result.Success,
                result.IntegratedOutput != null ? $"{result.IntegratedOutput.AgentOutputs.Count} agent(s) executed" : null,
                result.IntegratedOutput?.IntegratedResults,
                result.Conflicts.Count,
                result.Escalations.Count));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RunCopilotTaskAsync(
        [FromBody] CopilotTaskRequest request,
        [FromServices] Orchestrator orchestrator,
        [FromServices] ICopilotTaskStore copilotTaskStore,
        [FromServices] IDataDecisionAuditLog? auditLog,
        [FromServices] IAccessBoundary? accessBoundary,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Task))
            return Results.BadRequest(new ProblemDetails { Title = "Task is required" });

        var taskId = Guid.NewGuid().ToString("D");
        var submittedAt = DateTimeOffset.UtcNow;
        await copilotTaskStore.StoreAsync(new CopilotTaskRecord
        {
            TaskId = taskId,
            Task = request.Task.Trim(),
            SubmittedAt = submittedAt,
            CompletedAt = null,
            Success = false,
            Summary = null,
            Error = null
        }, cancellationToken);

        try
        {
            var result = await orchestrator.OrchestrateAsync(request.Task, cancellationToken);
            var auditCount = Math.Clamp(request.AuditCount <= 0 ? 25 : request.AuditCount, 1, 200);
            var recentAudit = auditLog?.GetRecent(auditCount) ?? [];
            var summary = result.IntegratedOutput != null ? $"{result.IntegratedOutput.AgentOutputs.Count} agent(s) executed" : null;
            await copilotTaskStore.StoreAsync(new CopilotTaskRecord
            {
                TaskId = taskId,
                Task = request.Task.Trim(),
                SubmittedAt = submittedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Success = result.Success,
                Summary = summary,
                Error = null
            }, cancellationToken);
            return Results.Ok(new CopilotTaskResponse(
                taskId,
                result.Success,
                summary,
                result.IntegratedOutput?.IntegratedResults,
                accessBoundary?.IsObservationPaused ?? false,
                recentAudit));
        }
        catch (Exception ex)
        {
            await copilotTaskStore.StoreAsync(new CopilotTaskRecord
            {
                TaskId = taskId,
                Task = request.Task.Trim(),
                SubmittedAt = submittedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Success = false,
                Summary = null,
                Error = ex.Message
            }, cancellationToken);
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ListCopilotTasksAsync(
        [FromServices] ICopilotTaskStore copilotTaskStore,
        [FromQuery] int maxCount = 50,
        [FromQuery] DateTimeOffset? since = null,
        CancellationToken cancellationToken = default)
    {
        maxCount = Math.Clamp(maxCount <= 0 ? 50 : maxCount, 1, 500);
        var tasks = await copilotTaskStore.QueryAsync(maxCount, since, cancellationToken);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetStatusAsync(
        [FromServices] IServiceProvider services,
        [FromServices] IAccessBoundary? accessBoundary,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var modeStore = services.GetService<IAggressivenessModeStore>();
        var mode = modeStore?.GetMode() ?? BackgroundAgentAggressivenessMode.Active;
        var registry = services.GetService<IBackgroundAgentRegistry>();
        var activeAgents = registry?.GetAll().Count(a => a.State == BackgroundAgentState.Running) ?? 0;
        var totalAgents = registry?.GetAll().Count ?? 0;
        var activePack = accessBoundary?.GetActivePolicyPack();
        return Results.Ok(new StatusResponse(
            mode.ToString(),
            "Nexo API is running",
            totalAgents,
            activeAgents,
            activePack?.Id,
            activePack?.Version));
    }

    private static async Task<IResult> BuildImageAsync(
        [FromBody] ExecutionBuildRequest request,
        [FromServices] IExecutionPlatform executionPlatform,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.DockerfilePath) || string.IsNullOrWhiteSpace(request?.ImageTag) || string.IsNullOrWhiteSpace(request?.ContextPath))
            return Results.BadRequest(new ProblemDetails { Title = "DockerfilePath, ImageTag, and ContextPath are required" });

        try
        {
            var result = await executionPlatform.BuildImageAsync(
                request.DockerfilePath,
                request.ImageTag,
                request.ContextPath,
                request.BuildArgs,
                null,
                cancellationToken);
            return Results.Ok(new ExecutionBuildResponse(result.Success, result.ErrorMessage, result.Duration.TotalMilliseconds));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RunContainerAsync(
        [FromBody] ExecutionRunRequest request,
        [FromServices] IExecutionPlatform executionPlatform,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.ImageTag) || request?.Command == null)
            return Results.BadRequest(new ProblemDetails { Title = "ImageTag and Command are required" });

        try
        {
            var result = await executionPlatform.RunContainerAsync(
                request.ImageTag,
                request.Command,
                request.EnvironmentVariables,
                request.VolumeMounts,
                request.WorkingDirectory,
                cancellationToken);
            return Results.Ok(new ExecutionRunResponse(result.Success, result.ExitCode, result.StandardOutput, result.StandardError, result.ContainerId, result.Duration.TotalMilliseconds));
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetCapabilitiesAsync(
        [FromServices] INodeCapabilityRuntime runtime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        var manifest = runtime.GetCapabilityManifest();
        return Results.Ok(ToDto(manifest));
    }

    private static async Task<IResult> RunDirectorWorkflowAsync(
        [FromBody] DirectorRunRequest request,
        [FromServices] Orchestrator orchestrator,
        [FromServices] IMediator mediator,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Goal))
            return Results.BadRequest(new ProblemDetails { Title = "Goal is required" });

        var dailiesPath = ResolveDailiesPath(configuration);
        Directory.CreateDirectory(dailiesPath);

        DirectorDailyEntry? previousDaily = null;
        if (!string.IsNullOrWhiteSpace(request.ContinueFromDailyId))
        {
            if (!IsValidDailyId(request.ContinueFromDailyId))
                return Results.BadRequest(new ProblemDetails { Title = "ContinueFromDailyId is invalid" });

            var previousPath = Path.Combine(dailiesPath, $"{request.ContinueFromDailyId}.json");
            if (File.Exists(previousPath))
            {
                previousDaily = await TryReadDailyAsync(previousPath, cancellationToken);
            }
        }

        var prompt = BuildDirectorPrompt(request, previousDaily);
        var success = false;
        string? summary;
        string? orchestrationError = null;
        string? integratedOutputJson = null;

        try
        {
            var orchestrationResult = await orchestrator.OrchestrateAsync(prompt, cancellationToken);
            success = orchestrationResult.Success;
            summary = orchestrationResult.IntegratedOutput != null
                ? $"{orchestrationResult.IntegratedOutput.AgentOutputs.Count} agent(s) executed"
                : "No integrated output generated";
            integratedOutputJson = SerializeForDaily(orchestrationResult.IntegratedOutput?.IntegratedResults);
        }
        catch (Exception ex)
        {
            summary = "Orchestration failed before completion";
            orchestrationError = ex.Message;
        }

        ValidationResponse? validation = null;
        if (request.RunValidation)
        {
            try
            {
                var validationResult = await mediator.Send(new RunValidationCommand(request.ValidationFilter), cancellationToken);
                validation = new ValidationResponse(
                    validationResult.Passed,
                    validationResult.Message,
                    validationResult.TestsRun,
                    validationResult.TestsPassed,
                    validationResult.TestsFailed);
            }
            catch (Exception ex)
            {
                validation = new ValidationResponse(
                    false,
                    $"Validation failed: {ex.Message}",
                    0,
                    0,
                    0);
            }
        }

        var dailyId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var entry = new DirectorDailyEntry(
            dailyId,
            DateTimeOffset.UtcNow,
            request.Goal.Trim(),
            request.Notes?.Trim(),
            request.ContinueFromDailyId,
            success,
            summary,
            integratedOutputJson,
            validation,
            orchestrationError);

        var dailyPath = Path.Combine(dailiesPath, $"{dailyId}.json");
        await File.WriteAllTextAsync(dailyPath, JsonSerializer.Serialize(entry, DailySerializerOptions), cancellationToken);

        return Results.Ok(new DirectorRunResponse(
            entry.Success,
            entry.DailyId,
            dailyPath,
            entry.Summary,
            entry.IntegratedOutputJson,
            entry.Validation,
            entry.OrchestrationError,
            entry.ContinueFromDailyId));
    }

    private static async Task<IResult> ListDailiesAsync(
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var dailiesPath = ResolveDailiesPath(configuration);
        if (!Directory.Exists(dailiesPath))
            return Results.Ok(Array.Empty<DirectorDailySummary>());

        var summaries = new List<DirectorDailySummary>();
        foreach (var file in Directory.EnumerateFiles(dailiesPath, "*.json"))
        {
            var entry = await TryReadDailyAsync(file, cancellationToken);
            if (entry is null)
                continue;

            summaries.Add(new DirectorDailySummary(
                entry.DailyId,
                entry.CreatedAtUtc,
                entry.Goal,
                entry.Success,
                entry.Summary,
                entry.ContinueFromDailyId));
        }

        return Results.Ok(summaries.OrderByDescending(x => x.CreatedAtUtc));
    }

    private static async Task<IResult> GetDailyAsync(
        string dailyId,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!IsValidDailyId(dailyId))
            return Results.BadRequest(new ProblemDetails { Title = "dailyId is invalid" });

        var dailiesPath = ResolveDailiesPath(configuration);
        var dailyPath = Path.Combine(dailiesPath, $"{dailyId}.json");
        if (!File.Exists(dailyPath))
            return Results.NotFound(new ProblemDetails { Title = "dailyId not found" });

        var entry = await TryReadDailyAsync(dailyPath, cancellationToken);
        if (entry is null)
            return Results.NotFound(new ProblemDetails { Title = "daily entry is unreadable" });

        return Results.Ok(entry);
    }

    private static string ResolveDailiesPath(IConfiguration configuration)
    {
        var configuredPath = configuration["NEXO_DAILIES_PATH"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = configuration["Nexo:DailiesPath"];

        configuredPath ??= Path.Combine(AppContext.BaseDirectory, "dailies");
        return Path.GetFullPath(configuredPath);
    }

    private static bool IsValidDailyId(string dailyId)
    {
        if (string.IsNullOrWhiteSpace(dailyId))
            return false;

        if (dailyId.Contains("..", StringComparison.Ordinal))
            return false;

        return dailyId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private static async Task<DirectorDailyEntry?> TryReadDailyAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<DirectorDailyEntry>(stream, DailySerializerOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildDirectorPrompt(DirectorRunRequest request, DirectorDailyEntry? previousDaily)
    {
        if (previousDaily is null)
            return request.Goal.Trim();

        var previousSummary = previousDaily.Summary ?? "No prior summary";
        return
            $"Continue from daily {previousDaily.DailyId}. " +
            $"Previous goal: {previousDaily.Goal}. " +
            $"Previous summary: {previousSummary}. " +
            $"Director notes for this iteration: {request.Goal.Trim()}";
    }

    private static string? SerializeForDaily(object? payload)
    {
        if (payload is null)
            return null;

        try
        {
            return JsonSerializer.Serialize(payload, DailySerializerOptions);
        }
        catch
        {
            return payload.ToString();
        }
    }

    private static NodeCapabilityManifestDto ToDto(NodeCapabilityManifest manifest)
    {
        return new NodeCapabilityManifestDto
        {
            NodeId = manifest.NodeId,
            Tier = manifest.Tier switch
            {
                NodeTier.Core => NodeTierDto.Core,
                NodeTier.Standard => NodeTierDto.Standard,
                NodeTier.Micro => NodeTierDto.Micro,
                _ => NodeTierDto.Nano
            },
            Platform = manifest.Platform switch
            {
                PlatformType.Windows => PlatformTypeDto.Windows,
                PlatformType.macOS => PlatformTypeDto.macOS,
                PlatformType.Linux => PlatformTypeDto.Linux,
                PlatformType.iOS => PlatformTypeDto.iOS,
                PlatformType.Android => PlatformTypeDto.Android,
                _ => PlatformTypeDto.Unknown
            },
            HotModelIds = manifest.HotModelIds,
            AvailableModelIds = manifest.AvailableModelIds,
            SupportedCapabilities = manifest.SupportedCapabilities.Select(cap => cap switch
            {
                TaskCapability.CodeGeneration => TaskCapabilityDto.CodeGeneration,
                TaskCapability.Embeddings => TaskCapabilityDto.Embeddings,
                TaskCapability.Vision => TaskCapabilityDto.Vision,
                TaskCapability.Reasoning => TaskCapabilityDto.Reasoning,
                TaskCapability.Classification => TaskCapabilityDto.Classification,
                _ => TaskCapabilityDto.TextGeneration
            }).ToArray(),
            AcceptingRemoteWork = manifest.AcceptingRemoteWork,
            GeneratedAt = manifest.GeneratedAt
        };
    }

    private static IResult GetSecurityAdvisory(IOptions<NexoSecurityOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        if (!Enum.TryParse<NexoExposureProfile>(options.ExposureProfile, true, out var profile))
            profile = NexoExposureProfile.Localhost;

        var (summary, hints) = SecurityAdvisoryContent.For(profile);
        var custom = string.IsNullOrWhiteSpace(options.CustomAdvisory) ? null : options.CustomAdvisory.Trim();
        return Results.Ok(new SecurityAdvisoryResponse(
            profile.ToString(),
            summary,
            hints,
            custom,
            options.ShowAdvisoryInPortal));
    }

    private static async Task<IResult> GetBackgroundAgentSummaryAsync(
        [FromServices] IServiceProvider services,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        var registry = services.GetService<IBackgroundAgentRegistry>();
        var modeStore = services.GetService<IAggressivenessModeStore>();
        var mode = modeStore?.GetMode() ?? BackgroundAgentAggressivenessMode.Active;
        var agents = registry?.GetAll() ?? [];
        var summaries = agents.Select(agent => new BackgroundAgentSnapshot(
            agent.Config.Id,
            agent.Config.Name,
            agent.Config.Role,
            agent.State.ToString(),
            agent.ExecutionCount,
            agent.SuccessCount,
            agent.FailureCount,
            agent.LastStartedAt,
            agent.LastCompletedAt,
            agent.LastError))
            .OrderBy(s => s.AgentId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Results.Ok(new BackgroundAgentSummaryResponse(mode.ToString(), summaries, summaries.Length));
    }

    private static async Task<IResult> GetTrustDashboardAsync(
        [FromServices] IDataDecisionAuditLog? auditLog,
        [FromServices] IAccessBoundary? accessBoundary,
        [FromQuery] int count = 25,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        count = Math.Clamp(count, 1, 200);

        var paused = accessBoundary?.IsObservationPaused ?? false;
        var audit = auditLog?.GetRecent(count) ?? [];
        var byType = audit
            .GroupBy(entry => entry.EventType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return Results.Ok(new TrustDashboardResponse(
            AccessBoundaryRegistered: accessBoundary != null,
            AuditLogRegistered: auditLog != null,
            IsPaused: paused,
            RecentAudit: audit,
            AuditByType: byType));
    }

    private static async Task<IResult> SetTrustPauseAsync(
        [FromBody] TrustPauseRequest request,
        [FromServices] IAccessBoundary? accessBoundary,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        if (accessBoundary == null)
            return Results.BadRequest(new ProblemDetails { Title = "Access boundary is not registered." });

        accessBoundary.SetPause(request.Paused);
        return Results.Ok(new TrustBoundaryMutationResponse(
            true,
            "pause",
            request.Paused ? "paused" : "resumed"));
    }

    private static async Task<IResult> SetTrustRuleAsync(
        [FromBody] TrustRuleRequest request,
        [FromServices] IAccessBoundary? accessBoundary,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        if (accessBoundary == null)
            return Results.BadRequest(new ProblemDetails { Title = "Access boundary is not registered." });

        if (string.IsNullOrWhiteSpace(request.Category) && string.IsNullOrWhiteSpace(request.Source))
            return Results.BadRequest(new ProblemDetails { Title = "Category or Source is required." });

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            accessBoundary.SetCategoryAllowed(request.Category.Trim(), request.Allowed);
            return Results.Ok(new TrustBoundaryMutationResponse(true, "category", request.Allowed ? "allowed" : "denied"));
        }

        accessBoundary.SetSourceAllowed(request.Source!.Trim(), request.Allowed);
        return Results.Ok(new TrustBoundaryMutationResponse(true, "source", request.Allowed ? "allowed" : "denied"));
    }

    private static async Task<IResult> QueryKnowledgeAsync(
        [FromServices] IKnowledgeQueryService queryService,
        [FromQuery] string? sources = null,
        [FromQuery] string? dataType = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int maxCount = 100,
        [FromQuery] int offset = 0,
        [FromQuery] DateTimeOffset? since = null,
        [FromQuery] DateTimeOffset? until = null,
        CancellationToken cancellationToken = default)
    {
        var selectedSources = ParseKnowledgeSources(sources);
        var request = new KnowledgeQueryRequest
        {
            Since = since,
            Until = until,
            DataType = dataType,
            EventType = eventType,
            MaxCount = maxCount,
            Offset = offset,
            Sources = selectedSources
        };
        var result = await queryService.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static IReadOnlyList<KnowledgeSource> ParseKnowledgeSources(string? sources)
    {
        if (string.IsNullOrWhiteSpace(sources))
            return Array.Empty<KnowledgeSource>();

        return sources
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => Enum.TryParse<KnowledgeSource>(token, true, out var parsed) ? parsed : (KnowledgeSource?)null)
            .Where(parsed => parsed.HasValue)
            .Select(parsed => parsed!.Value)
            .Distinct()
            .ToArray();
    }

    private static async Task<IResult> GetTrustStatusAsync(
        [FromServices] IAccessBoundary accessBoundary,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        var activePack = accessBoundary.GetActivePolicyPack();
        return Results.Ok(new TrustStatusResponse(
            accessBoundary.IsObservationPaused,
            accessBoundary.IsObservationPaused ? "Observation paused" : "Observing",
            activePack?.Id,
            activePack?.Version));
    }
}

// Request/Response DTOs
public sealed record AgentRequest(string AgentName, string? InputFilePath);
public sealed record AgentResponse(bool Success, string? Message, object? Output);

public sealed record ValidationRequest(string? Filter);
public sealed record ValidationResponse(bool Passed, string? Message, int TotalTests, int PassedTests, int FailedTests);

public sealed record OrchestrationRequest(
    string Request,
    string? RuntimeSpecJson = null,
    string? PreferModel = null,
    string? Provider = null,
    string? BarrierLevel = null,
    string? PreferredRegion = null);
public sealed record OrchestrationResponse(
    bool Success,
    string? Summary,
    object? Output,
    int? Conflicts = null,
    int? Escalations = null,
    string? ErrorCode = null);
public sealed record CopilotTaskRequest(string Task, int AuditCount = 25);
public sealed record CopilotTaskResponse(
    string TaskId,
    bool Success,
    string? Summary,
    object? Output,
    bool IsTrustPaused,
    IReadOnlyList<Nexo.Core.Application.Trust.Models.DataDecisionAuditEntry> RecentAudit);

public sealed record StatusResponse(
    string Mode,
    string Message,
    int TotalAgents,
    int ActiveAgents,
    string? ActivePackId,
    string? ActivePackVersion);

public sealed record ExecutionBuildRequest(string DockerfilePath, string ImageTag, string ContextPath, Dictionary<string, string>? BuildArgs = null);
public sealed record ExecutionBuildResponse(bool Success, string? ErrorMessage, double DurationMs);

public sealed record ExecutionRunRequest(string ImageTag, string[] Command, Dictionary<string, string>? EnvironmentVariables = null, Dictionary<string, string>? VolumeMounts = null, string? WorkingDirectory = null);
public sealed record ExecutionRunResponse(bool Success, int ExitCode, string StandardOutput, string StandardError, string? ContainerId, double DurationMs);
public sealed record TrustStatusResponse(bool IsPaused, string Status, string? ActivePolicyPackId, string? ActivePolicyPackVersion);

public sealed record DirectorRunRequest(
    string Goal,
    string? Notes = null,
    bool RunValidation = true,
    string? ValidationFilter = null,
    string? ContinueFromDailyId = null);

public sealed record DirectorRunResponse(
    bool Success,
    string DailyId,
    string DailyPath,
    string? Summary,
    string? IntegratedOutputJson,
    ValidationResponse? Validation,
    string? OrchestrationError,
    string? ContinuedFromDailyId);

public sealed record DirectorDailySummary(
    string DailyId,
    DateTimeOffset CreatedAtUtc,
    string Goal,
    bool Success,
    string? Summary,
    string? ContinueFromDailyId);

public sealed record DirectorDailyEntry(
    string DailyId,
    DateTimeOffset CreatedAtUtc,
    string Goal,
    string? Notes,
    string? ContinueFromDailyId,
    bool Success,
    string? Summary,
    string? IntegratedOutputJson,
    ValidationResponse? Validation,
    string? OrchestrationError);

public sealed record BackgroundAgentSummaryResponse(
    string Mode,
    IReadOnlyList<BackgroundAgentSnapshot> Agents,
    int TotalAgents);

public sealed record BackgroundAgentSnapshot(
    string AgentId,
    string Name,
    string Role,
    string State,
    int ExecutionCount,
    int SuccessCount,
    int FailureCount,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastError);

public sealed record TrustDashboardResponse(
    bool AccessBoundaryRegistered,
    bool AuditLogRegistered,
    bool IsPaused,
    IReadOnlyList<Nexo.Core.Application.Trust.Models.DataDecisionAuditEntry> RecentAudit,
    IReadOnlyDictionary<string, int> AuditByType);

public sealed record TrustPauseRequest(bool Paused);

public sealed record TrustRuleRequest(string? Category, string? Source, bool Allowed);

public sealed record TrustBoundaryMutationResponse(bool Ok, string Target, string State);
