using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using NodeCapabilityRuntimeImpl = Nexo.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Nexo.Infrastructure.NodeCapabilityRuntime.Scoring;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>Tests for ncr telemetry regression.</summary>
public sealed class NcrTelemetryRegressionTests
{
    [Fact]
    public async Task SelectModel_EnsureReady_RecordOutcome_EmitsExpectedMetrics()
    {
        var metrics = new InMemoryMetricsCollector();
        var policy = new LinuxPolicy();
        var model = new ModelDescriptor
        {
            Id = "phi3-mini",
            Provider = "ollama",
            ProviderModelId = "phi3-mini",
            State = ModelState.Warm,
            QualityScore = 0.8f,
            SpeedScore = 0.8f,
            MinRAMRequiredBytes = 256 * 1024 * 1024,
            Capabilities = new HashSet<TaskCapability> { TaskCapability.TextGeneration },
            SupportedPlatforms = new HashSet<PlatformType> { PlatformType.Linux }
        };

        var backend = new StubModelServingBackend();
        var lifecycle = new DefaultModelLifecycleManager(backend);
        var runtime = new NodeCapabilityRuntimeImpl(
            new FixedHardwareProfiler(new NodeProfile
            {
                Platform = PlatformType.Linux,
                Tier = NodeTier.Standard,
                AvailableRAMBytes = 8L * 1024 * 1024 * 1024,
                TotalRAMBytes = 16L * 1024 * 1024 * 1024,
                ThermalState = ThermalState.Nominal
            }),
            policy,
            lifecycle,
            new ModelScoringService(policy),
            Options.Create(new NodeCapabilityRuntimeOptions
            {
                NodeId = "telemetry-node",
                DefaultModels = [model]
            }),
            NullLogger<NodeCapabilityRuntimeImpl>.Instance,
            metrics);

        var resolution = await runtime.SelectModelAsync(new TaskContext
        {
            RequiredCapability = TaskCapability.TextGeneration,
            Quality = QualityRequirement.Medium,
            Latency = LatencyRequirement.Interactive,
            Privacy = PrivacyBoundary.Standard
        });
        await runtime.EnsureModelReadyAsync(resolution.Model);
        await runtime.RecordOutcomeAsync(
            resolution,
            new BrickExecutionOutcome
            {
                Succeeded = true,
                Duration = TimeSpan.FromMilliseconds(55)
            });

        metrics.Counters.Should().ContainKey("ncr.model_resolution.target.Local");
        metrics.Counters.Should().ContainKey("ncr.model_resolution.reason.BestLocalFit");
        metrics.Counters.Should().ContainKey("ncr.model_load.success");
        metrics.Counters.Should().ContainKey("ncr.outcome.success");
        metrics.ExecutionTimes.Should().ContainKey("ncr.model_resolution.duration");
        metrics.ExecutionTimes.Should().ContainKey("ncr.model_load.duration");
        metrics.ExecutionTimes.Should().ContainKey("ncr.outcome.duration");
    }

    /// <summary>Tests for fixed hardware profiler.</summary>
    private sealed class FixedHardwareProfiler : Nexo.Core.Application.NodeCapabilityRuntime.Ports.IHardwareProfiler
    {
        private readonly NodeProfile _profile;

        public FixedHardwareProfiler(NodeProfile profile)
        {
            _profile = profile;
        }

        /// <summary>Capture async.</summary>
        /// <param name="default">Default.</param>
        public Task<NodeProfile> CaptureAsync(CancellationToken ct = default) => Task.FromResult(_profile);
    }

    /// <summary>Tests for stub model serving backend.</summary>
    private sealed class StubModelServingBackend : Nexo.Core.Application.NodeCapabilityRuntime.Ports.IModelServingBackend
    {
        public BackendType BackendType => BackendType.Ollama;

        /// <summary>Returns whether  available async.</summary>
        /// <param name="default">Default.</param>
        public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<InferenceResult> RunInferenceAsync(InferenceRequest request, CancellationToken ct = default)
            => Task.FromResult(new InferenceResult { Output = "ok", Duration = TimeSpan.FromMilliseconds(1), TokenCount = 1 });

        /// <summary>Load model async.</summary>
        /// <param name="modelId">Model id.</param>
        /// <param name="default">Default.</param>
        public Task LoadModelAsync(string modelId, CancellationToken ct = default) => Task.CompletedTask;

        /// <summary>Unload model async.</summary>
        /// <param name="modelId">Model id.</param>
        /// <param name="default">Default.</param>
        public Task UnloadModelAsync(string modelId, CancellationToken ct = default) => Task.CompletedTask;

        public Task PullModelAsync(string modelId, IProgress<PullProgress>? progress = null, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<string>> ListLoadedModelsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);
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
                Counters = new Dictionary<string, long>(Counters)
            });
    }
}
