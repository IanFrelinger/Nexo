using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly string _nodeId;
    private readonly List<ModelDescriptor> _models;
    private readonly SimpleObservable<ConstraintUpdate> _constraintChanges = new();
    private readonly Dictionary<(string ModelId, TaskCapability Capability), float> _qualityFeedback = new();
    private NodeProfile _currentProfile = new();

    public NodeCapabilityRuntime(
        IHardwareProfiler profiler,
        IPlatformPolicy policy,
        IModelLifecycleManager lifecycle,
        ModelScoringService scoring,
        IOptions<NodeCapabilityRuntimeOptions> options,
        ILogger<NodeCapabilityRuntime> logger)
    {
        _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _scoring = scoring ?? throw new ArgumentNullException(nameof(scoring));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _nodeId = string.IsNullOrWhiteSpace(opts.NodeId) ? Environment.MachineName : opts.NodeId;
        _models = opts.DefaultModels.Count > 0
            ? new List<ModelDescriptor>(opts.DefaultModels)
            : DefaultModelSuite.CreateForPlatform(policy.Platform);
    }

    public NodeProfile CurrentProfile => _currentProfile;

    public IReadOnlyList<ModelDescriptor> AvailableModels => _models.AsReadOnly();

    public IObservable<ConstraintUpdate> ConstraintChanges => _constraintChanges;

    public async Task<ModelResolution> SelectModelAsync(TaskContext context, CancellationToken ct = default)
    {
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
            return new ModelResolution
            {
                Target = context.Privacy == PrivacyBoundary.LocalOnly ? InferenceTarget.Local : InferenceTarget.Escalate,
                Reason = context.Privacy == PrivacyBoundary.LocalOnly
                    ? ResolutionReason.ForcedLocal
                    : ResolutionReason.EscalatedInsufficientMemory
            };
        }

        var best = candidates[0].Model;

        if (context.Privacy == PrivacyBoundary.LocalOnly)
        {
            return new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Local,
                Reason = ResolutionReason.ForcedLocal
            };
        }

        if (!canRunNow)
        {
            return new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Escalate,
                Reason = ResolutionReason.EscalatedPolicyBlocked
            };
        }

        if (IsQualityInsufficient(context.Quality, best.QualityScore))
        {
            return new ModelResolution
            {
                Model = best,
                Target = InferenceTarget.Escalate,
                Reason = ResolutionReason.EscalatedQualityRequirement
            };
        }

        return new ModelResolution
        {
            Model = best,
            Target = InferenceTarget.Local,
            Reason = ResolutionReason.BestLocalFit
        };
    }

    public Task EnsureModelReadyAsync(ModelDescriptor model, CancellationToken ct = default)
        => _lifecycle.EnsureLoadedAsync(model, ct);

    public Task<NodeTier> GetTierAsync() => Task.FromResult(_currentProfile.Tier);

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
        }

        return next;
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

internal sealed class SimpleObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new();
    private readonly object _gate = new();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (observer is null) throw new ArgumentNullException(nameof(observer));
        lock (_gate)
        {
            _observers.Add(observer);
        }

        return new Subscription(_observers, _gate, observer);
    }

    public void Publish(T value)
    {
        List<IObserver<T>> snapshot;
        lock (_gate)
        {
            snapshot = new List<IObserver<T>>(_observers);
        }

        foreach (var observer in snapshot)
        {
            observer.OnNext(value);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly object _gate;
        private IObserver<T>? _observer;

        public Subscription(List<IObserver<T>> observers, object gate, IObserver<T> observer)
        {
            _observers = observers;
            _gate = gate;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observer is null) return;
            lock (_gate)
            {
                _observers.Remove(_observer);
            }

            _observer = null;
        }
    }
}
