using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Lifecycle states of a RunPod serverless job.
/// Mirrors the RunPod API status field.  <see cref="JobStatus.IsTerminal"/>
/// uses this to decide when polling should stop.
/// </summary>
public enum RunPodJobState
{
    /// <summary>Job is waiting in the RunPod queue.</summary>
    Queued,

    /// <summary>Job is actively executing on a GPU instance.</summary>
    Running,

    /// <summary>Job finished successfully.</summary>
    Completed,

    /// <summary>Job terminated with an error.</summary>
    Failed,

    /// <summary>Returned when the RunPod API response cannot be parsed or
    /// the status field is missing; treated as non-terminal so polling continues.</summary>
    Unknown
}
