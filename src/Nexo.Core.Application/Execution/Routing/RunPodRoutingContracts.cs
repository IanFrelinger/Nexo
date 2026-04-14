using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Classifies the GPU horsepower required by a job.
/// <para>
/// Used by <see cref="ICapabilityRouter"/> to match <see cref="JobRequirements"/>
/// against available execution targets.  The numeric ordering (<c>None</c> &lt;
/// <c>Low</c> &lt; … &lt; <c>Extreme</c>) is significant: the router will accept
/// any target whose advertised class is ≥ the requirement.
/// </para>
/// </summary>
public enum GpuComputeClass
{
    /// <summary>No GPU needed — CPU-only workloads.</summary>
    None = 0,
    /// <summary>Entry-level GPU (e.g. ≤ 8 GB VRAM).</summary>
    Low = 1,
    /// <summary>Mid-range GPU (e.g. A4000 / 16 GB class).</summary>
    Medium = 2,
    /// <summary>High-end GPU (e.g. A100 40 GB).</summary>
    High = 3,
    /// <summary>Multi-GPU or top-tier single GPU (e.g. A100 80 GB, H100).</summary>
    Extreme = 4
}

/// <summary>
/// Lifecycle states of a RunPod serverless job.
/// Mirrors the RunPod API status field.  <see cref="JobStatus.IsTerminal"/>
/// uses this to decide when polling should stop.
/// </summary>
public enum RunPodJobState
{
    Queued,
    Running,
    Completed,
    Failed,
    /// <summary>Returned when the RunPod API response cannot be parsed or
    /// the status field is missing; treated as non-terminal so polling continues.</summary>
    Unknown
}

/// <summary>
/// Caller-specified preference for where a job should execute when the local
/// node cannot satisfy <see cref="JobRequirements"/>.
/// <para>
/// <b>Routing model:</b> The <see cref="ICapabilityRouter"/> evaluates targets
/// in this order: local NCR → peer network → RunPod cloud.
/// <c>RemoteExecutionPreference</c> lets callers constrain or override that
/// default cascade.
/// </para>
/// </summary>
public enum RemoteExecutionPreference
{
    /// <summary>Follow the system-default routing cascade (local → peer → cloud).</summary>
    UseSystemDefault = 0,
    /// <summary>Skip peer network; go directly to RunPod cloud if local fails.</summary>
    CloudOnly = 1,
    /// <summary>Try the peer network first; fall back to cloud if no peer can serve.</summary>
    PreferPeerNetwork = 2,
    /// <summary>Only use the peer network; fail if no peer is available.</summary>
    PeerNetworkOnly = 3
}

/// <summary>
/// Represents a live RunPod GPU instance that Nexo has provisioned.
/// Instances are ephemeral — spun up for a job batch and terminated
/// after idle timeout or explicit <see cref="IRunPodClient.TerminateInstance"/>.
/// </summary>
public sealed record RunPodInstance
{
    public string InstanceId { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string GpuType { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Opaque handle returned by <see cref="IRunPodClient.DispatchJob"/> and used
/// for subsequent <see cref="IRunPodClient.PollJobStatus"/> /
/// <see cref="IRunPodClient.PullResults"/> calls.
/// </summary>
public sealed record JobHandle
{
    public string InstanceId { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
}

/// <summary>
/// Snapshot of a RunPod job's current state.
/// <see cref="IsTerminal"/> is true for <see cref="RunPodJobState.Completed"/>
/// and <see cref="RunPodJobState.Failed"/> — callers should stop polling once
/// it returns true.
/// </summary>
public sealed record JobStatus
{
    public RunPodJobState State { get; init; } = RunPodJobState.Unknown;
    public string? Message { get; init; }

    public bool IsTerminal =>
        State == RunPodJobState.Completed ||
        State == RunPodJobState.Failed;
}

/// <summary>
/// Payload sent to a RunPod serverless endpoint (or relayed to a peer).
/// <see cref="GenerationParameters"/> carries provider-specific knobs (top-p,
/// repetition penalty, etc.) that are passed through opaquely.
/// <see cref="IsPriority"/> bumps the job to the front of the RunPod queue
/// when the endpoint supports priority scheduling.
/// </summary>
public sealed record RunPodJobPayload
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> GenerationParameters { get; init; } = new Dictionary<string, object>();
    public string OutputFormat { get; init; } = "text/plain";
    public bool IsPriority { get; init; }
}

/// <summary>
/// Describes what a job needs from its execution target.
/// <para>
/// <see cref="ICapabilityRouter.ResolveExecutionTarget"/> compares these
/// requirements against the local node's capabilities (via NCR) and, if the
/// local node is insufficient, against available peers and RunPod GPU tiers.
/// </para>
/// <para>
/// <see cref="RemoteExecutionPreference"/> interacts with the trust tier
/// system: <c>PeerNetworkOnly</c> will only route to peers whose trust
/// level satisfies the policy in <c>RunPodBrickConfig.PeerTrustPolicy</c>.
/// </para>
/// </summary>
public sealed record JobRequirements
{
    /// <summary>Minimum VRAM required; used to filter GPU tiers.</summary>
    public long MinimumVramBytes { get; init; }

    /// <summary>Compute class floor for this job.</summary>
    public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.Low;

    /// <summary>Estimated wall-clock duration; influences queue-depth routing decisions.</summary>
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Target model identifier (must be loadable on the chosen target).</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Hints that this job is low-priority and can be deferred to
    /// cheaper / slower execution targets (e.g. spot instances).</summary>
    public bool IsOvernightOrBackground { get; init; }

    /// <summary>Overrides the system-default routing cascade for this job.
    /// See <see cref="RemoteExecutionPreference"/> for options.</summary>
    public RemoteExecutionPreference RemoteExecutionPreference { get; init; } = RemoteExecutionPreference.UseSystemDefault;
}

/// <summary>
/// Unified result returned by any <see cref="IBrickExecutor"/>, regardless
/// of whether execution was local, peer, or cloud.  <see cref="IsRemote"/>
/// and <see cref="Provider"/> let callers trace provenance for auditing.
/// </summary>
public sealed record GenerationExecutionResult
{
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public string OutputPath { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public bool IsRemote { get; init; }
    public string Summary { get; init; } = string.Empty;
}

/// <summary>
/// Configuration for the RunPod execution backend.
/// Bound from the <c>Nexo:RunPod</c> configuration section.
/// <para>
/// Peer-network fields (<c>EnablePeerNetworkRouting</c>, <c>PeerTrustPolicy</c>,
/// <c>TrustedPeerIdsCsv</c>, etc.) control the middle tier of the routing
/// cascade (local → <b>peer</b> → cloud).  When <c>EnablePeerNetworkRouting</c>
/// is false the router skips peers entirely and falls through to RunPod cloud.
/// </para>
/// <para>
/// <b>Trust interaction:</b> <c>PeerTrustPolicy</c> works with the trust
/// subsystem registered in <c>NexoServiceCollectionExtensions</c>.
/// "trusted-preferred" tries trusted peers first but allows untrusted as
/// fallback; "trusted-only" rejects untrusted peers entirely.
/// </para>
/// </summary>
public sealed class RunPodBrickConfig
{
    public const string SectionName = "Nexo:RunPod";

    /// <summary>RunPod API key.  Required for cloud execution; ignored for peer-only routing.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>GPU tier to request (e.g. "NVIDIA_A4000").
    /// See <see cref="NexoDefaults.RunPodDefaultGpuTier"/>.</summary>
    public string PreferredGpuTier { get; set; } = NexoDefaults.RunPodDefaultGpuTier;

    /// <summary>Maximum time to wait for a single RunPod job before aborting.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(NexoDefaults.RunPodDefaultTimeoutMinutes);

    /// <summary>Interval between job-status poll requests to the RunPod API.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPollingIntervalSeconds);

    /// <summary>Local directory where RunPod job outputs are staged before
    /// being returned to the caller.  Defaults to a temp-directory subfolder.</summary>
    public string OutputStagingPath { get; set; } = Path.Combine(Path.GetTempPath(), "nexo-runpod");

    /// <summary>When the RunPod queue depth exceeds this value, the router
    /// will attempt peer-network routing (if enabled) before falling back
    /// to cloud.  See <see cref="NexoDefaults.RunPodDefaultQueueDepthThreshold"/>.</summary>
    public int QueueDepthThreshold { get; set; } = NexoDefaults.RunPodDefaultQueueDepthThreshold;

    /// <summary>Base URL for the RunPod API.</summary>
    public string BaseUrl { get; set; } = NexoDefaults.RunPodDefaultBaseUrl;

    /// <summary>Master switch for peer-network routing.  When false, the
    /// routing cascade skips peers and goes directly to RunPod cloud.</summary>
    public bool EnablePeerNetworkRouting { get; set; }

    /// <summary>When true and peers are available, the router prefers a
    /// peer over RunPod cloud even if the cloud queue is empty.</summary>
    public bool PreferPeerNetworkOverCloud { get; set; } = true;

    /// <summary>Trust policy applied to peer selection.
    /// Values: "trusted-preferred" (default), "trusted-only", "any".
    /// Interacts with <c>TrustedPeerIdsCsv</c> / <c>UntrustedPeerIdsCsv</c>.</summary>
    public string PeerTrustPolicy { get; set; } = NexoDefaults.RunPodDefaultPeerTrustPolicy;

    /// <summary>Comma-separated list of explicitly trusted peer node IDs.</summary>
    public string TrustedPeerIdsCsv { get; set; } = string.Empty;

    /// <summary>Comma-separated list of explicitly untrusted (blocked) peer node IDs.</summary>
    public string UntrustedPeerIdsCsv { get; set; } = string.Empty;

    /// <summary>NCR capability ID used to discover peers that advertise
    /// generation capability routing.</summary>
    public string PeerCapabilityId { get; set; } = NexoDefaults.RunPodDefaultPeerCapabilityId;

    /// <summary>Brick ID dispatched when routing a generation job to a peer.</summary>
    public string PeerRoutingBrickId { get; set; } = NexoDefaults.RunPodDefaultPeerRoutingBrickId;

    /// <summary>Timeout for individual peer-to-peer requests.</summary>
    public TimeSpan PeerRequestTimeout { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerRequestTimeoutSeconds);

    /// <summary>How often the background task refreshes the known-peers list.</summary>
    public TimeSpan PeerDiscoveryInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerDiscoveryIntervalSeconds);
}

/// <summary>
/// Abstraction over the RunPod serverless API.
/// <para>
/// Lifecycle: <see cref="SpinUpInstance"/> → <see cref="DispatchJob"/> →
/// poll <see cref="PollJobStatus"/> until terminal → <see cref="PullResults"/>
/// → <see cref="TerminateInstance"/>.
/// </para>
/// <para>
/// Implementations may pool instances across callers; instance IDs are opaque
/// and should not be parsed.
/// </para>
/// </summary>
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

/// <summary>
/// Common interface for any component that can execute a generation job,
/// whether locally, on a peer, or in the cloud.  <see cref="ILocalExecutor"/>
/// and <see cref="IPeerExecutor"/> are marker sub-interfaces that let DI
/// and the router distinguish execution venue without runtime type-checks.
/// </summary>
public interface IBrickExecutor
{
    Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for executors that run jobs on the local node's GPU / CPU.
/// Registered in DI only when the NCR module detects sufficient local capability.
/// </summary>
public interface ILocalExecutor : IBrickExecutor
{
}

/// <summary>
/// Marker interface for executors that forward jobs to a peer node in the
/// mesh network.  Trust policy (<c>RunPodBrickConfig.PeerTrustPolicy</c>)
/// is enforced before dispatch.
/// </summary>
public interface IPeerExecutor : IBrickExecutor
{
}

/// <summary>
/// Discriminated union representing where a job will actually run.
/// Produced by <see cref="ICapabilityRouter.ResolveExecutionTarget"/>
/// and consumed by the pipeline stage that dispatches the job.
/// <para>
/// <c>Reason</c> is a human-readable explanation of why this target was
/// chosen (e.g. "local GPU meets VRAM requirement", "peer abc123 trusted,
/// lower latency than cloud") — useful for debugging routing decisions.
/// </para>
/// </summary>
public abstract record ExecutionTarget
{
    private ExecutionTarget()
    {
    }

    /// <summary>Job will execute on the local node.</summary>
    public sealed record Local(ILocalExecutor Executor, string Reason) : ExecutionTarget;

    /// <summary>Job will execute on a remote target (peer or RunPod cloud).</summary>
    public sealed record Remote(IBrickExecutor Executor, string Reason) : ExecutionTarget;
}

/// <summary>
/// Decides where a generation job should execute based on
/// <see cref="JobRequirements"/> and the current system state.
/// <para>
/// <b>Routing cascade:</b>
/// <list type="number">
///   <item>Check local NCR capabilities (VRAM, compute class).</item>
///   <item>If local is insufficient and peer routing is enabled, query
///         known peers filtered by <c>RunPodBrickConfig.PeerTrustPolicy</c>.</item>
///   <item>Fall back to RunPod cloud, selecting GPU tier from
///         <c>RunPodBrickConfig.PreferredGpuTier</c>.</item>
/// </list>
/// <see cref="JobRequirements.RemoteExecutionPreference"/> can override
/// this cascade per-job.
/// </para>
/// </summary>
public interface ICapabilityRouter
{
    ExecutionTarget ResolveExecutionTarget(JobRequirements requirements);
}
