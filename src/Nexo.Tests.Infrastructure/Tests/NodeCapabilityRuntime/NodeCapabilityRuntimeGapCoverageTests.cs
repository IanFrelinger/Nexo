using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using NodeCapabilityRuntimeImpl = Nexo.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Backends;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for node capability runtime gap coverage.</summary>
public class NodeCapabilityRuntimeGapCoverageTests
{
    private static ModelDescriptor TextModel => new()
    {
        Id = "phi3-mini",
        Provider = "ollama",
        ProviderModelId = "phi3-mini",
        State = ModelState.Warm,
        QualityScore = 0.62f,
        SpeedScore = 0.8f,
        MinRAMRequiredBytes = 256 * 1024 * 1024,
        Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
        SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux },
    };

    private static ModelDescriptor HotEmbeddingModel => new()
    {
        Id = "embed-model",
        Provider = "ollama",
        ProviderModelId = "nomic-embed-text",
        State = ModelState.Hot,
        QualityScore = 0.75f,
        SpeedScore = 0.95f,
        MinRAMRequiredBytes = 128 * 1024 * 1024,
        Capabilities = new HashSet<TaskCapability> { TaskCapability.Embeddings },
        SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux },
    };

    [Fact]
    public void Constructor_uses_platform_default_models_when_none_configured()
    {
        var runtime = CreateRuntime(
            policy: new MacOsPolicy(),
            models: [],
            profile: new NodeProfile { Platform = PlatformType.macOS, Tier = NodeTier.Standard });

        runtime.AvailableModels.Should().NotBeEmpty();
        runtime.AvailableModels.Select(m => m.Id).Should().Contain("phi3-mini");
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var options = Options.Create(new NodeCapabilityRuntimeOptions { DefaultModels = [TextModel] });
        var lifecycle = new DefaultModelLifecycleManager(new NullModelServingBackend());
        var scoring = new ModelScoringService(new LinuxPolicy());
        var profiler = new FixedHardwareProfiler(new NodeProfile { Platform = PlatformType.Linux });

        var actProfiler = () => new NodeCapabilityRuntimeImpl(
            null!, new LinuxPolicy(), lifecycle, scoring, options, NullLogger<NodeCapabilityRuntimeImpl>.Instance);
        var actPolicy = () => new NodeCapabilityRuntimeImpl(
            profiler, null!, lifecycle, scoring, options, NullLogger<NodeCapabilityRuntimeImpl>.Instance);
        var actLifecycle = () => new NodeCapabilityRuntimeImpl(
            profiler, new LinuxPolicy(), null!, scoring, options, NullLogger<NodeCapabilityRuntimeImpl>.Instance);
        var actScoring = () => new NodeCapabilityRuntimeImpl(
            profiler, new LinuxPolicy(), lifecycle, null!, options, NullLogger<NodeCapabilityRuntimeImpl>.Instance);
        var actOptions = () => new NodeCapabilityRuntimeImpl(
            profiler, new LinuxPolicy(), lifecycle, scoring, null!, NullLogger<NodeCapabilityRuntimeImpl>.Instance);
        var actLogger = () => new NodeCapabilityRuntimeImpl(
            profiler, new LinuxPolicy(), lifecycle, scoring, options, null!);

        actProfiler.Should().Throw<ArgumentNullException>();
        actPolicy.Should().Throw<ArgumentNullException>();
        actLifecycle.Should().Throw<ArgumentNullException>();
        actScoring.Should().Throw<ArgumentNullException>();
        actOptions.Should().Throw<ArgumentNullException>();
        actLogger.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SelectModelAsync_escalates_when_no_candidates_and_not_local_only()
    {
        var runtime = CreateRuntime(models: [TextModel]);
        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.Vision,
            Privacy = PrivacyBoundary.Standard,
        });

        resolution.Target.Should().Be(InferenceTarget.Escalate);
        resolution.Reason.Should().Be(ResolutionReason.EscalatedInsufficientMemory);
    }

    [Fact]
    public async Task SelectModelAsync_forces_local_when_no_candidates_and_local_only()
    {
        var runtime = CreateRuntime(models: [TextModel]);
        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.Vision,
            Privacy = PrivacyBoundary.LocalOnly,
        });

        resolution.Target.Should().Be(InferenceTarget.Local);
        resolution.Reason.Should().Be(ResolutionReason.ForcedLocal);
    }

    [Fact]
    public async Task SelectModelAsync_forces_local_when_privacy_requires_it()
    {
        var runtime = CreateRuntime(models: [TextModel]);
        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Privacy = PrivacyBoundary.LocalOnly,
            Quality = QualityRequirement.Low,
        });

        resolution.Target.Should().Be(InferenceTarget.Local);
        resolution.Reason.Should().Be(ResolutionReason.ForcedLocal);
        resolution.Model.Id.Should().Be(TextModel.Id);
    }

    [Fact]
    public async Task SelectModelAsync_escalates_when_policy_blocks_inference()
    {
        var runtime = CreateRuntime(
            models: [TextModel],
            profile: new NodeProfile
            {
                Platform = PlatformType.Linux,
                Tier = NodeTier.Standard,
                GPUUtilizationPercent = 99f,
                AvailableRAMBytes = 8L * 1024 * 1024 * 1024,
            });

        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Privacy = PrivacyBoundary.Standard,
            Quality = QualityRequirement.Low,
        });

        resolution.Target.Should().Be(InferenceTarget.Escalate);
        resolution.Reason.Should().Be(ResolutionReason.EscalatedPolicyBlocked);
    }

    [Fact]
    public async Task SelectModelAsync_escalates_when_quality_requirement_not_met()
    {
        var runtime = CreateRuntime(models: [TextModel]);
        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Privacy = PrivacyBoundary.Standard,
            Quality = QualityRequirement.Maximum,
        });

        resolution.Target.Should().Be(InferenceTarget.Escalate);
        resolution.Reason.Should().Be(ResolutionReason.EscalatedQualityRequirement);
    }

    [Fact]
    public async Task SelectModelAsync_publishes_constraint_updates_when_profile_changes()
    {
        var initial = new NodeProfile
        {
            Platform = PlatformType.Linux,
            Tier = NodeTier.Standard,
            AvailableRAMBytes = 8L * 1024 * 1024 * 1024,
            ThermalState = ThermalState.Nominal,
        };
        var updated = initial with
        {
            AvailableRAMBytes = 1L * 1024 * 1024 * 1024,
            ThermalState = ThermalState.Serious,
        };

        var profiler = new SequenceHardwareProfiler(initial, updated);
        var metrics = new InMemoryMetricsCollector();
        var runtime = CreateRuntime(
            models: [TextModel],
            profiler: profiler,
            metrics: metrics);

        var updates = new List<ConstraintUpdate>();
        using var subscription = runtime.ConstraintChanges.Subscribe(new CollectObserver<ConstraintUpdate>(updates));

        await runtime.SelectModelAsync(new TaskContext { RequiredCapability = TaskCapability.TextGeneration });
        await runtime.SelectModelAsync(new TaskContext { RequiredCapability = TaskCapability.TextGeneration });

        updates.Should().Contain(u =>
            u.Reason == "MaterialConstraintChange" &&
            u.Current.ThermalState == ThermalState.Serious);
        metrics.Counters.Should().ContainKey("ncr.profile.constraint_change");
    }

    [Fact]
    public async Task EnsureModelReadyAsync_records_error_metrics_when_load_fails()
    {
        var backend = new Mock<IModelServingBackend>();
        backend.Setup(b => b.ListLoadedModelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        backend.Setup(b => b.LoadModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("load failed"));

        var metrics = new InMemoryMetricsCollector();
        var runtime = CreateRuntime(
            models: [TextModel],
            lifecycle: new DefaultModelLifecycleManager(backend.Object),
            metrics: metrics);

        var act = () => runtime.EnsureModelReadyAsync(TextModel);
        await act.Should().ThrowAsync<InvalidOperationException>();

        metrics.Counters.Should().ContainKey("ncr.model_load.error");
        metrics.ExecutionTimes.Should().ContainKey("ncr.model_load.duration");
    }

    [Fact]
    public async Task GetCapabilityManifest_and_GetTierAsync_reflect_runtime_state()
    {
        var runtime = CreateRuntime(models: [TextModel, HotEmbeddingModel]);
        await runtime.SelectModelAsync(new TaskContext { RequiredCapability = TaskCapability.TextGeneration });

        var manifest = runtime.GetCapabilityManifest();
        manifest.NodeId.Should().Be("gap-node");
        manifest.HotModelIds.Should().Contain("embed-model");
        manifest.AvailableModelIds.Should().Contain("phi3-mini");
        manifest.SupportedCapabilities.Should().Contain(TaskCapability.TextGeneration);

        (await runtime.GetTierAsync()).Should().Be(NodeTier.Standard);
    }

    [Fact]
    public async Task RecordOutcomeAsync_skips_empty_model_and_null_outcome()
    {
        var metrics = new InMemoryMetricsCollector();
        var runtime = CreateRuntime(models: [TextModel], metrics: metrics);

        await runtime.RecordOutcomeAsync(
            new ModelResolution { Model = new ModelDescriptor { Id = "" } },
            new BrickExecutionOutcome { Succeeded = true });
        await runtime.RecordOutcomeAsync(
            new ModelResolution { Model = TextModel },
            null!);

        metrics.Counters.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordOutcomeAsync_records_failure_timeout_and_slow_duration()
    {
        var metrics = new InMemoryMetricsCollector();
        var runtime = CreateRuntime(models: [TextModel], metrics: metrics);
        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Quality = QualityRequirement.Low,
        });

        await runtime.RecordOutcomeAsync(
            resolution,
            new BrickExecutionOutcome
            {
                Succeeded = false,
                TimedOut = true,
                Duration = TimeSpan.FromSeconds(45),
            });

        metrics.Counters.Should().ContainKey("ncr.outcome.failure");
        metrics.Counters.Should().ContainKey("ncr.outcome.timeout");
        metrics.ExecutionTimes.Should().ContainKey("ncr.outcome.duration");
    }

    private static NodeCapabilityRuntimeImpl CreateRuntime(
        IPlatformPolicy? policy = null,
        IEnumerable<ModelDescriptor>? models = null,
        NodeProfile? profile = null,
        IHardwareProfiler? profiler = null,
        IModelLifecycleManager? lifecycle = null,
        IMetricsCollector? metrics = null)
    {
        return new NodeCapabilityRuntimeImpl(
            profiler ?? new FixedHardwareProfiler(profile ?? new NodeProfile
            {
                Platform = PlatformType.Linux,
                Tier = NodeTier.Standard,
                AvailableRAMBytes = 8L * 1024 * 1024 * 1024,
                TotalRAMBytes = 16L * 1024 * 1024 * 1024,
                ThermalState = ThermalState.Nominal,
            }),
            policy ?? new LinuxPolicy(),
            lifecycle ?? new DefaultModelLifecycleManager(new NullModelServingBackend()),
            new ModelScoringService(policy ?? new LinuxPolicy()),
            Options.Create(new NodeCapabilityRuntimeOptions
            {
                NodeId = "gap-node",
                DefaultModels = models?.ToList() ?? [TextModel],
            }),
            NullLogger<NodeCapabilityRuntimeImpl>.Instance,
            metrics);
    }

    /// <summary>Tests for fixed hardware profiler.</summary>
    private sealed class FixedHardwareProfiler(NodeProfile profile) : IHardwareProfiler
    {
        /// <summary>Capture async.</summary>
        /// <param name="default">Default.</param>
        public Task<NodeProfile> CaptureAsync(CancellationToken ct = default) => Task.FromResult(profile);
    }

    /// <summary>Tests for sequence hardware profiler.</summary>
    private sealed class SequenceHardwareProfiler(NodeProfile first, NodeProfile second) : IHardwareProfiler
    {
        private int _calls;

        public Task<NodeProfile> CaptureAsync(CancellationToken ct = default)
            => Task.FromResult(Interlocked.Increment(ref _calls) == 1 ? first : second);
    }

    /// <summary>Tests for collect observer.</summary>
    private sealed class CollectObserver<T>(List<T> sink) : IObserver<T>
    {
        /// <summary>On completed.</summary>
        public void OnCompleted() { }
        /// <summary>On error.</summary>
        /// <param name="error">Error.</param>
        public void OnError(Exception error) => throw error;
        /// <summary>On next.</summary>
        /// <param name="value">Value.</param>
        public void OnNext(T value) => sink.Add(value);
    }

    /// <summary>Tests for in memory metrics collector.</summary>
    private sealed class InMemoryMetricsCollector : IMetricsCollector
    {
        /// <summary>Execution times.</summary>
        public Dictionary<string, TimeSpan> ExecutionTimes { get; } = new(StringComparer.Ordinal);
        /// <summary>Counters.</summary>
        public Dictionary<string, long> Counters { get; } = new(StringComparer.Ordinal);

        public void RecordExecutionTime(string operationName, TimeSpan duration)
        {
            ExecutionTimes[operationName] = ExecutionTimes.TryGetValue(operationName, out var current)
                ? current + duration
                : duration;
        }

        public void IncrementCounter(string counterName, int value = 1)
        {
            Counters[counterName] = Counters.TryGetValue(counterName, out var current)
                ? current + value
                : value;
        }

        public Task<MetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new MetricsSnapshot
            {
                ExecutionTimes = new Dictionary<string, TimeSpan>(ExecutionTimes),
                Counters = new Dictionary<string, long>(Counters),
            });
    }
}
