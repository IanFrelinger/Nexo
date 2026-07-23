using Nexo.Core.Application.Scaling.Models;

namespace Nexo.Core.Application.Scaling.Ports;

/// <summary>
/// Pure policy that turns demand + current replicas into a desired replica count.
/// Kept separate from <see cref="IWorkloadScaler"/> so providers stay swappable
/// without rewriting autoscale math.
/// </summary>
public interface IWorkloadScalePolicy
{
    /// <summary>Evaluates whether to change replica count for a workload.</summary>
    WorkloadScaleDecision Evaluate(WorkloadScaleContext context);
}
