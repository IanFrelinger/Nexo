using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;

namespace Nexo.Infrastructure.NodeCapabilityRuntime;

/// <summary>
/// Default node capability runtime implementation.
/// </summary>
public sealed class NodeCapabilityRuntime : INodeCapabilityRuntime
{
    private readonly IHardwareProfiler _profiler;
    private readonly IPlatformPolicy _policy;
    private readonly IModelLifecycleManager _lifecycle;
    private readonly ModelScoringService _scoring;
    private readonly ILogger<NodeCapabilityRuntime> _logger;
    private readonly IMetricsCollector? _metricsCollector;
    private readonly string _nodeId;
    private readonly List<ModelDescriptor> _models;
    private readonly SimpleObservable<ConstraintUpdate> _constraintChanges = new();
    private readonly Dictionary<(string ModelId, TaskCapability Capability), float> _qualityFeedback = new();
    private NodeProfile _currentProfile = new();

    /// <summary>Initializes a new node capability runtime.</summary>
    public NodeCapabilityRuntime(
        IHardwareProfiler profiler,
        IPlatformPolicy policy,
        IModelLifecycleManager lifecycle,
        ModelScoringService scoring,
        IOptions<NodeCapabilityRuntimeOptions> options,
        ILogger<NodeCapabilityRuntime> logger,
        IMetricsCollector? metricsCollector = null)
    {
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _scoring = scoring ?? throw new ArgumentNullException(nameof(scoring));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _nodeId = string.IsNullOrWhiteSpace(opts.NodeId) ? Environment.MachineName : opts.NodeId;
        _models = opts.DefaultModels.Count > 0
            ? new List<ModelDescriptor>(opts.DefaultModels)
            : DefaultModelSuite.CreateForPlatform(policy.Platform);
    }

    /// <summary>Current profile.</summary>
    public NodeProfile CurrentProfile => _currentProfile;

    /// <summary>Available models.</summary>
    public IReadOnlyList<ModelDescriptor> AvailableModels => _models.AsReadOnly();

    /// <summary>Constraint changes.</summary>
    public IObservable<ConstraintUpdate> ConstraintChanges => _constraintChanges;

    /// <summary>Select model asynchronously.</summary>
    public async Task<ModelResolution> SelectModelAsync(TaskContext context, CancellationToken ct = default)
    {
        var selectStarted = DateTimeOffset.UtcNow;
        _currentProfile = await RefreshProfileAsync(ct).ConfigureAwait(false);
        var profile = _currentProfile;
        var canRunNow = _policy.CanRunInferenceNow(profile);

        var candidates = _models
            .Where(model => _scoring.ScoreModel(model, context, profile) > float.MinValue)
            .Select(model => new
            {
                Model = ApplyFeedback(model, context.RequiredCapability),
                Score = _scoring.ScoreModel(ApplyFeedback(model, context.RequiredCapability), context, profile)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        if (candidates.Count == 0)
        {
            var resolution = new ModelResolution
            {
                Target = context.Privacy == PrivacyBoundary.LocalOnly ? InferenceTarget.Local : InferenceTarget.Escalate,
                Reason = context.Privacy == PrivacyBoundary.LocalOnly
                    ? ResolutionReason.ForcedLocal
                    : ResolutionReason.EscalatedInsufficientMemory
            };
            RecordResolutionTelemetry(resolution, DateTimeOffset.UtcNow - selectStarted);
            return resolution;
        }

        var best = candidates[0].Model;

        if (context.Privacy == PrivacyBoundary.LocalOnly)
        {
            var resolution = new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Local,
                Reason = ResolutionReason.ForcedLocal
            };
            RecordResolutionTelemetry(resolution, DateTimeOffset.UtcNow - selectStarted);
            return resolution;
        }

        if (!canRunNow)
        {
            var resolution = new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Escalate,
                Reason = ResolutionReason.EscalatedPolicyBlocked
            };
            RecordResolutionTelemetry(resolution, DateTimeOffset.UtcNow - selectStarted);
            return resolution;
        }

        if (IsQualityInsufficient(context.Quality, best.QualityScore))
        {
            var resolution = new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Escalate,
                Reason = ResolutionReason.EscalatedQualityRequirement
            };
            RecordResolutionTelemetry(resolution, DateTimeOffset.UtcNow - selectStarted);
            return resolution;
        }

        var bestFit = new ModelResolution
        {
            Model = best,
            Target = InferenceTarget.Local,
            Reason = ResolutionReason.BestLocalFit
        };
        RecordResolutionTelemetry(bestFit, DateTimeOffset.UtcNow - selectStarted);
        return bestFit;
    }

    /// <summary>Ensure model ready asynchronously.</summary>
    public async Task EnsureModelReadyAsync(ModelDescriptor model, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        try
        {
            await _lifecycle.EnsureLoadedAsync(model, ct).ConfigureAwait(false);
            _metricsCollector?.IncrementCounter("ncr.model_load.success");
            _metricsCollector?.RecordExecutionTime("ncr.model_load.duration", DateTimeOffset.UtcNow - started);
        }
        catch
        {
            _metricsCollector?.IncrementCounter("ncr.model_load.error");
            _metricsCollector?.RecordExecutionTime("ncr.model_load.duration", DateTimeOffset.UtcNow - started);
            throw;
        }
    }

    /// <summary>Get tier asynchronously.</summary>
    public Task<NodeTier> GetTierAsync() => Task.FromResult(_currentProfile.Tier);

    /// <summary>Gets capability manifest.</summary>
    public NodeCapabilityManifest GetCapabilityManifest()
    {
        var hotModelIds = _models.Where(m => m.State == ModelState.Hot).Select(m => m.Id).ToArray();
        var capabilitySet = _models
            .SelectMany(x => x.Capabilities)
            .Distinct()
            .ToArray();

        return new NodeCapabilityManifest
        {
            NodeId = _nodeId,
            Tier = _currentProfile.Tier,
            Platform = _currentProfile.Platform,
            HotModelIds = hotModelIds,
            AvailableModelIds = _models.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            SupportedCapabilities = capabilitySet,
            AcceptingRemoteWork = _policy.CanAdvertiseRemoteWork(_currentProfile),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Record outcome asynchronously.</summary>
    public Task RecordOutcomeAsync(ModelResolution resolution, BrickExecutionOutcome outcome, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (resolution.Model.Id.Length == 0 || outcome is null)
        {
            return Task.CompletedTask;
        }

        var capability = resolution.Model.Capabilities.FirstOrDefault();
        var key = (resolution.Model.Id, capability);
        _qualityFeedback.TryGetValue(key, out var currentOffset);

        var delta = 0f;
        if (outcome.Succeeded) delta += 0.02f;
        if (outcome.TimedOut) delta -= 0.03f;
        if (!outcome.Succeeded) delta -= 0.02f;
        if (outcome.Duration > TimeSpan.FromSeconds(30)) delta -= 0.01f;

        _qualityFeedback[key] = Math.Clamp(currentOffset + delta, -0.2f, 0.2f);
        _logger.LogDebug(
            "Recorded NCR outcome for model {ModelId} cap {Capability}: delta={Delta} new={Offset}",
            resolution.Model.Id,
            capability,
            delta,
            _qualityFeedback[key]);
        _metricsCollector?.IncrementCounter(outcome.Succeeded ? "ncr.outcome.success" : "ncr.outcome.failure");
        if (outcome.TimedOut)
        {
            _metricsCollector?.IncrementCounter("ncr.outcome.timeout");
        }
        _metricsCollector?.RecordExecutionTime("ncr.outcome.duration", outcome.Duration);
        return Task.CompletedTask;
    }

    private async Task<NodeProfile> RefreshProfileAsync(CancellationToken ct)
    {
        var next = await _profiler.CaptureAsync(ct).ConfigureAwait(false);
        if (_currentProfile != default &&
            (_currentProfile.Tier != next.Tier ||
             _currentProfile.AvailableRAMBytes != next.AvailableRAMBytes ||
             _currentProfile.AvailableVRAMBytes != next.AvailableVRAMBytes ||
             _currentProfile.ThermalState != next.ThermalState))
        {
            _constraintChanges.Publish(new ConstraintUpdate
            {
                Previous = _currentProfile,
                Current = next,
                Reason = "MaterialConstraintChange"
            });
            _metricsCollector?.IncrementCounter("ncr.profile.constraint_change");
        }

        return next;
    }

    private void RecordResolutionTelemetry(ModelResolution resolution, TimeSpan duration)
    {
        _metricsCollector?.RecordExecutionTime("ncr.model_resolution.duration", duration);
        _metricsCollector?.IncrementCounter($"ncr.model_resolution.target.{resolution.Target}");
        _metricsCollector?.IncrementCounter($"ncr.model_resolution.reason.{resolution.Reason}");
        _logger.LogInformation(
            "NCR model resolution completed: target={Target} reason={Reason} model={ModelId} duration_ms={DurationMs}",
            resolution.Target,
            resolution.Reason,
            resolution.Model.Id,
            duration.TotalMilliseconds);
    }

    private ModelDescriptor ApplyFeedback(ModelDescriptor model, TaskCapability capability)
    {
        if (!_qualityFeedback.TryGetValue((model.Id, capability), out var offset))
        {
            return model;
        }

        return model with { QualityScore = Math.Clamp(model.QualityScore + offset, 0f, 1f) };
    }

    private static bool IsQualityInsufficient(QualityRequirement requirement, float qualityScore)
    {
        var minimum = requirement switch
        {
            QualityRequirement.Maximum => 0.90f,
            QualityRequirement.High => 0.75f,
            QualityRequirement.Medium => 0.55f,
            _ => 0.0f
        };
        return qualityScore < minimum;
    }
}
