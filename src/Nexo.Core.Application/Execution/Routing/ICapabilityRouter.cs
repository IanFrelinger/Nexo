using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

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
    /// <summary>
    /// Resolves the execution target for a job given its resource requirements.
    /// </summary>
    /// <param name="requirements">VRAM, compute class, and routing preferences for the job.</param>
    /// <returns>Local or remote execution target with a human-readable routing reason.</returns>
    ExecutionTarget ResolveExecutionTarget(JobRequirements requirements);
}
