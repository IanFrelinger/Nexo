using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

public enum GpuComputeClass
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Extreme = 4
}

public enum RunPodJobState
{
    Queued,
    Running,
    Completed,
    Failed,
    Unknown
}

public enum RemoteExecutionPreference
{
    UseSystemDefault = 0,
    CloudOnly = 1,
    PreferPeerNetwork = 2,
    PeerNetworkOnly = 3
}

public sealed record RunPodInstance
{
    public string InstanceId { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string GpuType { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record JobHandle
{
    public string InstanceId { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
}

public sealed record JobStatus
{
    public RunPodJobState State { get; init; } = RunPodJobState.Unknown;
    public string? Message { get; init; }

    public bool IsTerminal =>
        State == RunPodJobState.Completed ||
        State == RunPodJobState.Failed;
}

public sealed record RunPodJobPayload
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> GenerationParameters { get; init; } = new Dictionary<string, object>();
    public string OutputFormat { get; init; } = "text/plain";
    public bool IsPriority { get; init; }
}

public sealed record JobRequirements
{
    public long MinimumVramBytes { get; init; }
    public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.Low;
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.FromMinutes(1);
    public string ModelId { get; init; } = string.Empty;
    public bool IsOvernightOrBackground { get; init; }
    public RemoteExecutionPreference RemoteExecutionPreference { get; init; } = RemoteExecutionPreference.UseSystemDefault;
}

public sealed record GenerationExecutionResult
{
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public string OutputPath { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public bool IsRemote { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed class RunPodBrickConfig
{
    public const string SectionName = "Nexo:RunPod";

    public string ApiKey { get; set; } = string.Empty;
    public string PreferredGpuTier { get; set; } = NexoDefaults.RunPodDefaultGpuTier;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(NexoDefaults.RunPodDefaultTimeoutMinutes);
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPollingIntervalSeconds);
    public string OutputStagingPath { get; set; } = Path.Combine(Path.GetTempPath(), "nexo-runpod");
    public int QueueDepthThreshold { get; set; } = NexoDefaults.RunPodDefaultQueueDepthThreshold;
    public string BaseUrl { get; set; } = NexoDefaults.RunPodDefaultBaseUrl;
    public bool EnablePeerNetworkRouting { get; set; }
    public bool PreferPeerNetworkOverCloud { get; set; } = true;
    public string PeerTrustPolicy { get; set; } = NexoDefaults.RunPodDefaultPeerTrustPolicy;
    public string TrustedPeerIdsCsv { get; set; } = string.Empty;
    public string UntrustedPeerIdsCsv { get; set; } = string.Empty;
    public string PeerCapabilityId { get; set; } = NexoDefaults.RunPodDefaultPeerCapabilityId;
    public string PeerRoutingBrickId { get; set; } = NexoDefaults.RunPodDefaultPeerRoutingBrickId;
    public TimeSpan PeerRequestTimeout { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerRequestTimeoutSeconds);
    public TimeSpan PeerDiscoveryInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerDiscoveryIntervalSeconds);
}

public interface IRunPodClient
{
    Task<Result<RunPodInstance>> SpinUpInstance(
        string modelId,
        string gpuType,
        CancellationToken cancellationToken = default);

    Task<Result<JobHandle>> DispatchJob(
        string instanceId,
        RunPodJobPayload payload,
        CancellationToken cancellationToken = default);

    Task<Result<JobStatus>> PollJobStatus(
        JobHandle jobHandle,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> PullResults(
        JobHandle jobHandle,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> TerminateInstance(
        string instanceId,
        CancellationToken cancellationToken = default);
}

public interface IBrickExecutor
{
    Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}

public interface ILocalExecutor : IBrickExecutor
{
}

public interface IPeerExecutor : IBrickExecutor
{
}

public abstract record ExecutionTarget
{
    private ExecutionTarget()
    {
    }

    public sealed record Local(ILocalExecutor Executor, string Reason) : ExecutionTarget;

    public sealed record Remote(IBrickExecutor Executor, string Reason) : ExecutionTarget;
}

public interface ICapabilityRouter
{
    ExecutionTarget ResolveExecutionTarget(JobRequirements requirements);
}
