using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution.Routing;

/// <summary>
/// Generation brick that transparently delegates to local or remote target.
/// </summary>
public sealed class CapabilityRoutingBrick : DomainBrick, IBrickExecutor
{
    private readonly ICapabilityRouter _router;
    private readonly ILogger<CapabilityRoutingBrick> _logger;

    /// <summary>Initializes a new capability routing brick.</summary>
    public CapabilityRoutingBrick(
        ICapabilityRouter router,
        ILogger<CapabilityRoutingBrick> logger)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "generation.capability-routing";
        Name = "Capability Routing Generation";
        Version = "1.0.0";
        Icon = "🧭";
        Category = BrickCategory.Generation;
        Description = "Default generation entry point that routes between local execution and RunPod.";
        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("payload", "RunPodJobPayload", "Typed generation payload", false),
                new BrickInputDefinition("modelId", "string", "Model id", false),
                new BrickInputDefinition("prompt", "string", "Prompt text", false),
                new BrickInputDefinition("parameters", "object", "Generation parameters", false),
                new BrickInputDefinition("outputFormat", "string", "Output format", false, "text/plain"),
                new BrickInputDefinition("priority", "bool", "Priority flag", false, false),
                new BrickInputDefinition("requirements", "JobRequirements", "Execution requirements", false),
                new BrickInputDefinition("minimumVramBytes", "long", "Minimum VRAM", false, 0L),
                new BrickInputDefinition("computeClass", "string", "Required compute class", false, GpuComputeClass.Low.ToString()),
                new BrickInputDefinition("estimatedDurationSeconds", "int", "Estimated duration in seconds", false, 60),
                new BrickInputDefinition("overnight", "bool", "Overnight/background flag", false, false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("success", "bool", "Execution success"),
                new BrickOutputDefinition("payload", "byte[]", "Generated payload bytes"),
                new BrickOutputDefinition("outputPath", "string", "Optional staged output path"),
                new BrickOutputDefinition("provider", "string", "Execution provider"),
                new BrickOutputDefinition("routedTarget", "string", "Local or Remote decision"),
                new BrickOutputDefinition("routingReason", "string", "Routing reason"),
                new BrickOutputDefinition("errorCode", "string", "Error code on failure"),
                new BrickOutputDefinition("errorMessage", "string", "Error message on failure")
            ]
        };
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "capability-router",
                Name = "Capability router",
                Description = "Routes generation jobs by NCR profile and job requirements",
                Executor = nameof(CapabilityRoutingBrick)
            }
        };
    }

    /// <summary>Execute asynchronously.</summary>
    public async Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var target = _router.ResolveExecutionTarget(requirements);
        IBrickExecutor executor;
        string routedTarget;
        string reason;
        if (target is ExecutionTarget.Local local)
        {
            executor = local.Executor;
            routedTarget = "Local";
            reason = local.Reason;
        }
        else if (target is ExecutionTarget.Remote remote)
        {
            executor = remote.Executor;
            routedTarget = "Remote";
            reason = remote.Reason;
        }
        else
        {
            return Result<GenerationExecutionResult>.Failure(
                "capability-routing.invalid_target",
                "Capability router returned an unknown execution target.");
        }

        _logger.LogInformation(
            "capability-routing audit decision={Decision} reason={Reason} model={ModelId} audit={AuditMode}",
            routedTarget,
            reason,
            payload.ModelId,
            context.AuditMode);

        var execution = await executor.ExecuteAsync(payload, requirements, context, cancellationToken).ConfigureAwait(false);
        if (execution.IsFailure || execution.Value is null)
        {
            return Result<GenerationExecutionResult>.Failure(
                execution.Error?.Code ?? "capability-routing.execution_failed",
                execution.Error?.Message ?? "Delegated execution failed.",
                execution.Error?.Detail);
        }

        return Result<GenerationExecutionResult>.Success(execution.Value with
        {
            IsRemote = string.Equals(routedTarget, "Remote", StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>Execute asynchronously.</summary>
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = ReadPayload(input);
        var requirements = ReadRequirements(input, payload.ModelId);
        var target = _router.ResolveExecutionTarget(requirements);
        var routedTarget = target is ExecutionTarget.Remote ? "Remote" : "Local";
        var reason = target is ExecutionTarget.Remote remote ? remote.Reason : ((ExecutionTarget.Local)target).Reason;

        var result = await ExecuteAsync(payload, requirements, context, cancellationToken).ConfigureAwait(false);
        var output = new BrickOutput();
        output.Set("routedTarget", routedTarget);
        output.Set("routingReason", reason);

        if (result.IsSuccess && result.Value is not null)
        {
            output.Set("success", true);
            output.Set("payload", result.Value.Payload);
            output.Set("outputPath", result.Value.OutputPath);
            output.Set("provider", result.Value.Provider);
            output.Summary = $"Generation completed via {routedTarget}: {reason}";
            return output;
        }

        output.Set("success", false);
        output.Set("errorCode", result.Error?.Code ?? "capability-routing.unknown");
        output.Set("errorMessage", result.Error?.Message ?? "Capability routing execution failed.");
        output.Summary = $"Generation failed via {routedTarget}: {result.Error?.Code ?? "capability-routing.unknown"}";
        return output;
    }

    private static RunPodJobPayload ReadPayload(BrickInput input)
    {
        var typed = input.Get<RunPodJobPayload?>("payload", null);
        if (typed is not null)
        {
            return typed;
        }

        return new RunPodJobPayload
        {
            ModelId = input.Get<string>("modelId", string.Empty) ?? string.Empty,
            Prompt = input.Get<string>("prompt", string.Empty) ?? string.Empty,
            GenerationParameters = input.Get<IReadOnlyDictionary<string, object>?>("parameters", null) ??
                                   new Dictionary<string, object>(),
            OutputFormat = input.Get<string>("outputFormat", "text/plain") ?? "text/plain",
            IsPriority = input.Get<bool>("priority", false)
        };
    }

    private static JobRequirements ReadRequirements(BrickInput input, string fallbackModelId)
    {
        var typed = input.Get<JobRequirements?>("requirements", null);
        if (typed is not null)
        {
            return typed;
        }

        var computeClassText = input.Get<string>("computeClass", GpuComputeClass.Low.ToString()) ?? GpuComputeClass.Low.ToString();
        var computeClass = Enum.TryParse<GpuComputeClass>(computeClassText, true, out var parsed)
            ? parsed
            : GpuComputeClass.Low;

        return new JobRequirements
        {
            ModelId = input.Get<string>("modelId", fallbackModelId) ?? fallbackModelId,
            MinimumVramBytes = input.Get<long>("minimumVramBytes", 0L),
            ComputeClass = computeClass,
            EstimatedDuration = TimeSpan.FromSeconds(Math.Max(1, input.Get<int>("estimatedDurationSeconds", 60))),
            IsOvernightOrBackground = input.Get<bool>("overnight", false)
        };
    }
}
