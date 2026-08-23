namespace Ashlar.Infrastructure.Scaling;

/// <summary>
/// Configuration for container workload scaling.
/// Bound from <c>Ashlar:WorkloadScaling</c>.
/// </summary>
public sealed class WorkloadScalerOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ashlar:WorkloadScaling";

    /// <summary>
    /// Active provider: <c>null</c>, <c>kubernetes</c>, or <c>compose</c>.
    /// Env override: <c>ASHLAR_WORKLOAD_SCALER</c>.
    /// </summary>
    public string Provider { get; set; } = "null";

    /// <summary>When false, scale mutations are rejected (reads may still work).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Kubernetes Deployment scaling via kubectl.</summary>
    public KubernetesWorkloadScalerOptions Kubernetes { get; set; } = new();

    /// <summary>Docker Compose service scaling.</summary>
    public ComposeWorkloadScalerOptions Compose { get; set; } = new();

    /// <summary>Optional host loop that applies <see cref="QueueDepthWorkloadScalePolicy"/>.</summary>
    public WorkloadAutoscaleOptions Autoscale { get; set; } = new();
}

/// <summary>Kubernetes-specific scaler options.</summary>
public sealed class KubernetesWorkloadScalerOptions
{
    /// <summary>Path to kubectl (default: kubectl on PATH).</summary>
    public string KubectlPath { get; set; } = "kubectl";

    /// <summary>Optional kubeconfig path.</summary>
    public string? Kubeconfig { get; set; }

    /// <summary>Default namespace when a workload omits one.</summary>
    public string Namespace { get; set; } = "default";

    /// <summary>Logical workload id → Deployment mapping.</summary>
    public Dictionary<string, KubernetesWorkloadBinding> Workloads { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Maps a logical workload to a Kubernetes Deployment.</summary>
public sealed class KubernetesWorkloadBinding
{
    /// <summary>Deployment name.</summary>
    public string Deployment { get; set; } = "";

    /// <summary>Namespace override (falls back to scaler default).</summary>
    public string? Namespace { get; set; }

    /// <summary>Human-readable name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Minimum replicas.</summary>
    public int MinReplicas { get; set; } = 0;

    /// <summary>Maximum replicas.</summary>
    public int MaxReplicas { get; set; } = 20;
}

/// <summary>Docker Compose scaler options.</summary>
public sealed class ComposeWorkloadScalerOptions
{
    /// <summary>Path to docker CLI.</summary>
    public string DockerPath { get; set; } = "docker";

    /// <summary>Compose file path (host-local).</summary>
    public string ComposeFile { get; set; } = "docker-compose.yml";

    /// <summary>Optional project directory passed to <c>docker compose</c>.</summary>
    public string? ProjectDirectory { get; set; }

    /// <summary>Logical workload id → Compose service mapping.</summary>
    public Dictionary<string, ComposeWorkloadBinding> Workloads { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Maps a logical workload to a Compose service.</summary>
public sealed class ComposeWorkloadBinding
{
    /// <summary>Compose service name.</summary>
    public string Service { get; set; } = "";

    /// <summary>Human-readable name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Minimum replicas.</summary>
    public int MinReplicas { get; set; } = 0;

    /// <summary>Maximum replicas.</summary>
    public int MaxReplicas { get; set; } = 20;
}

/// <summary>Background autoscale loop options.</summary>
public sealed class WorkloadAutoscaleOptions
{
    /// <summary>When true, register <see cref="ElasticWorkloadAutoscaleService"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>Polling interval.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Workload ids to autoscale (empty = all configured for the active provider).</summary>
    public List<string> WorkloadIds { get; set; } = new();

    /// <summary>Scale up when queue depth is at or above this value.</summary>
    public int ScaleUpQueueDepth { get; set; } = 5;

    /// <summary>Scale down when queue depth is at or below this value.</summary>
    public int ScaleDownQueueDepth { get; set; } = 0;

    /// <summary>Replica step when scaling up.</summary>
    public int ScaleUpStep { get; set; } = 1;

    /// <summary>Replica step when scaling down.</summary>
    public int ScaleDownStep { get; set; } = 1;
}
