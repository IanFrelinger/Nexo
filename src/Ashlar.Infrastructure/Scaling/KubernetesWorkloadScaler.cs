using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Scaling.Models;
using Ashlar.Core.Application.Scaling.Ports;

namespace Ashlar.Infrastructure.Scaling;

/// <summary>
/// Scales Kubernetes Deployments via <c>kubectl</c>.
/// First-class provider — swap to Compose/ECS by changing <c>Ashlar:WorkloadScaling:Provider</c>.
/// </summary>
public sealed class KubernetesWorkloadScaler : IWorkloadScaler
{
    private readonly WorkloadScalerOptions _options;
    private readonly IProcessCommandRunner _runner;
    private readonly ILogger<KubernetesWorkloadScaler> _logger;

    /// <summary>Creates a Kubernetes workload scaler.</summary>
    public KubernetesWorkloadScaler(
        IOptions<WorkloadScalerOptions> options,
        IProcessCommandRunner runner,
        ILogger<KubernetesWorkloadScaler> logger)
    {
        _options = options.Value;
        _runner = runner;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => "kubernetes";

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var args = BuildBaseArgs("version", "--client=true");
        var result = await _runner.RunAsync(_options.Kubernetes.KubectlPath, args, cancellationToken)
            .ConfigureAwait(false);
        return result.Succeeded;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkloadDescriptor>> ListWorkloadsAsync(CancellationToken cancellationToken = default)
    {
        var list = _options.Kubernetes.Workloads
            .Select(kv => ToDescriptor(kv.Key, kv.Value))
            .ToList();
        return Task.FromResult<IReadOnlyList<WorkloadDescriptor>>(list);
    }

    /// <inheritdoc />
    public async Task<WorkloadReplicaSnapshot> GetReplicasAsync(
        string workloadId,
        CancellationToken cancellationToken = default)
    {
        var binding = RequireBinding(workloadId);
        var ns = ResolveNamespace(binding);
        var args = BuildBaseArgs(
            "get", "deployment", binding.Deployment,
            "-n", ns,
            "-o", "jsonpath={.spec.replicas},{.status.replicas},{.status.readyReplicas}");

        var result = await _runner.RunAsync(_options.Kubernetes.KubectlPath, args, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"kubectl get deployment failed for '{workloadId}': {result.StdErr}");
        }

        var parts = result.StdOut.Split(',', StringSplitOptions.TrimEntries);
        var desired = ParseInt(parts, 0);
        var current = ParseInt(parts, 1);
        var ready = ParseInt(parts, 2);
        return new WorkloadReplicaSnapshot(
            workloadId,
            desired,
            current,
            ready,
            DateTimeOffset.UtcNow,
            ProviderDetail: $"deployment/{binding.Deployment} ns={ns}");
    }

    /// <inheritdoc />
    public async Task<WorkloadScaleResult> ScaleAsync(
        WorkloadScaleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new WorkloadScaleResult(
                false,
                request.WorkloadId,
                0,
                request.DesiredReplicas,
                "Workload scaling is disabled (Ashlar:WorkloadScaling:Enabled=false).");
        }

        var binding = RequireBinding(request.WorkloadId);
        var previous = 0;
        try
        {
            previous = (await GetReplicasAsync(request.WorkloadId, cancellationToken).ConfigureAwait(false))
                .DesiredReplicas;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read previous replicas for {WorkloadId}", request.WorkloadId);
        }

        var desired = Math.Clamp(request.DesiredReplicas, binding.MinReplicas, binding.MaxReplicas);
        var ns = ResolveNamespace(binding);
        var args = BuildBaseArgs(
            "scale", $"deployment/{binding.Deployment}",
            $"--replicas={desired}",
            "-n", ns);

        var result = await _runner.RunAsync(_options.Kubernetes.KubectlPath, args, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return new WorkloadScaleResult(
                false,
                request.WorkloadId,
                previous,
                desired,
                $"kubectl scale failed: {result.StdErr}");
        }

        _logger.LogInformation(
            "Scaled Kubernetes workload {WorkloadId} deployment/{Deployment} {Previous} -> {Desired} ({Reason})",
            request.WorkloadId,
            binding.Deployment,
            previous,
            desired,
            request.Reason ?? "manual");

        return new WorkloadScaleResult(true, request.WorkloadId, previous, desired, result.StdOut);
    }

    private KubernetesWorkloadBinding RequireBinding(string workloadId)
    {
        if (!_options.Kubernetes.Workloads.TryGetValue(workloadId, out var binding) ||
            string.IsNullOrWhiteSpace(binding.Deployment))
        {
            throw new KeyNotFoundException(
                $"Unknown Kubernetes workload '{workloadId}'. Configure Ashlar:WorkloadScaling:Kubernetes:Workloads.");
        }

        return binding;
    }

    private string ResolveNamespace(KubernetesWorkloadBinding binding) =>
        string.IsNullOrWhiteSpace(binding.Namespace) ? _options.Kubernetes.Namespace : binding.Namespace!;

    private List<string> BuildBaseArgs(params string[] command)
    {
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.Kubernetes.Kubeconfig))
        {
            args.Add($"--kubeconfig={_options.Kubernetes.Kubeconfig}");
        }

        args.AddRange(command);
        return args;
    }

    private static WorkloadDescriptor ToDescriptor(string id, KubernetesWorkloadBinding binding) =>
        new(
            id,
            binding.DisplayName ?? binding.Deployment,
            binding.MinReplicas,
            binding.MaxReplicas,
            ProviderResourceHint: $"deployment/{binding.Deployment}");

    private static int ParseInt(string[] parts, int index)
    {
        if (index >= parts.Length || string.IsNullOrWhiteSpace(parts[index]))
            return 0;
        return int.TryParse(parts[index], out var value) ? value : 0;
    }
}
